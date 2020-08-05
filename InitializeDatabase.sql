-- medTODO counts involving `card_state <> 3` are going to be slightly wrong. They're using Card, and a Card can have multiple Cards.

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

CREATE TYPE public.notification_type AS ENUM (
    'DeckAddedStack',
    'DeckUpdatedStack',
    'DeckDeletedStack'
);


ALTER TYPE public.notification_type OWNER TO postgres;

CREATE FUNCTION public.fn_ctr_branch_insertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        default_branch_id integer NOT NULL := 0;
    BEGIN
        default_branch_id := (SELECT s.default_branch_id FROM stack s WHERE NEW.stack_id = s.id);
        IF ((NEW.name IS NOT NULL) AND (default_branch_id = NEW.id)) THEN
            RAISE EXCEPTION 'Default Branches must have a null Name. StackId#% with BranchId#% by UserId#% just attempted to be titled %', (NEW.stack_id), (NEW.id), (NEW.author_id), (NEW.name);
        ELSIF ((NEW.name IS NULL) AND (default_branch_id <> NEW.id)) THEN
            RAISE EXCEPTION 'Only Default Branches may have a null Name. StackId#% with BranchId#% by UserId#% just attempted to be titled %', (NEW.stack_id), (NEW.id), (NEW.author_id), (NEW.name);
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_ctr_branch_insertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_ctr_card_insertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF (1 < (SELECT COUNT(*) FROM (SELECT DISTINCT cc.leaf_id FROM card cc WHERE cc.user_id = NEW.user_id AND cc.stack_id = NEW.stack_id) _)) THEN
            RAISE EXCEPTION 'UserId #% with Card #% and Stack #% tried to have LeafId #%, but they already have LeafId #%',
            (NEW.user_id), (NEW.id), (NEW.stack_id), (NEW.leaf_id), (SELECT cc.leaf_id FROM card cc WHERE cc.user_id = NEW.user_id AND cc.stack_id = NEW.stack_id LIMIT 1);
        END IF;
		IF (TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND (OLD.leaf_id <> NEW.leaf_id OR OLD.index <> NEW.index))) THEN
		IF ((SELECT bi.max_index_inclusive FROM public.leaf bi WHERE bi.id = NEW.leaf_id) < NEW.index) THEN
			RAISE EXCEPTION 'UserId #% with Card #% tried to have index %, which exceeds the MaxIndexInclusive value of % on its LeafId #%', (NEW.user_id), (NEW.id), (NEW.index), (SELECT bi.max_index_inclusive FROM public.leaf bi WHERE bi.id = NEW.leaf_id), (NEW.leaf_id);
		END IF;
		END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_ctr_card_insertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_delete_received_notification(outer_notification_id integer, outer_receiver_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
    BEGIN
        WITH del_child AS (
            DELETE FROM public.received_notification rn
            WHERE  rn.notification_id = outer_notification_id
            AND    rn.receiver_id = outer_receiver_id
            RETURNING rn.notification_id, rn.receiver_id
        )
        DELETE FROM public.notification n
        USING  del_child x
        WHERE  n.id = x.notification_id
        AND NOT EXISTS (
            SELECT 1
            FROM   public.received_notification rn
            WHERE  rn.notification_id = x.notification_id
            AND    rn.receiver_id <> x.receiver_id
        );
    END
$$;


ALTER FUNCTION public.fn_delete_received_notification(outer_notification_id integer, outer_receiver_id integer) OWNER TO postgres;

COMMENT ON FUNCTION public.fn_delete_received_notification(outer_notification_id integer, outer_receiver_id integer) IS 'https://stackoverflow.com/a/15810159';


CREATE FUNCTION public.fn_tr_branch_afterinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        default_branch_id integer NOT NULL := 0;
    BEGIN
        IF (TG_OP = 'INSERT') THEN
            UPDATE stack s
            SET    default_branch_id = (NEW.id)
            WHERE (s.id = NEW.stack_id AND s.default_branch_id = 0);
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_branch_afterinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_card_afterinsertdeleteupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        new_is_public boolean NOT NULL := 'f';
        old_is_public boolean NOT NULL := 'f';
    BEGIN
		IF ((TG_OP = 'DELETE' AND OLD.index = 0 AND OLD.card_state <> 3) OR 
            (TG_OP = 'UPDATE' AND (OLD.leaf_id <> NEW.leaf_id
                                      OR (OLD.card_state <> 3 AND NEW.card_state = 3)))) THEN
            UPDATE	leaf ci
            SET     users = ci.users - 1
            WHERE	ci.id = OLD.leaf_id;
            UPDATE	branch b
            SET		users = b.users - 1
            WHERE	b.id = OLD.branch_id;
            UPDATE  stack stack
            SET     users = stack.users - 1
            WHERE stack.id = OLD.stack_id;
        END IF;
        IF ((TG_OP = 'INSERT' AND NEW.index = 0) OR
            (TG_OP = 'UPDATE' AND (OLD.leaf_id <> NEW.leaf_id
                                      OR (OLD.card_state = 3 AND NEW.card_state <> 3)))) THEN
            UPDATE	leaf ci
            SET     users = ci.users + 1
            WHERE	ci.id = NEW.leaf_id;
            UPDATE	branch b
            SET		users = b.users + 1
            WHERE	b.id = NEW.branch_id;
            UPDATE  stack stack
            SET     users = stack.users + 1
            WHERE stack.id = NEW.stack_id;
        END IF;
        new_is_public := COALESCE((SELECT is_public FROM public.deck WHERE id = NEW.deck_id), 'f');
        old_is_public := COALESCE((SELECT is_public FROM public.deck WHERE id = OLD.deck_id), 'f');
        IF (new_is_public OR old_is_public) THEN
            IF (TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND OLD.deck_id <> NEW.deck_id AND new_is_public)) THEN
                WITH notification_id AS (
                    INSERT INTO public.notification(sender_id, time_stamp,              type,          message,     stack_id,     branch_id,     leaf_id,     deck_id, gromplate_id, grompleaf_id)
                                            VALUES (NEW.user_id, (timezone('utc', now())), 'DeckAddedStack', NULL,     NEW.stack_id, NEW.branch_id, NEW.leaf_id, NEW.deck_id,  NULL,       NULL)
                    RETURNING id
                ) INSERT INTO public.received_notification(receiver_id, notification_id)
                                                 (SELECT df.follower_id, (SELECT id FROM notification_id)
                                                  FROM public.deck_follower df
                                                  WHERE df.deck_id = NEW.deck_id
                                                 );
            END IF;
            IF (TG_OP = 'UPDATE' AND OLD.leaf_id <> NEW.leaf_id) THEN
                WITH notification_id AS (
                    INSERT INTO public.notification(sender_id, time_stamp,              type,          message,     stack_id,     branch_id,     leaf_id,     deck_id, gromplate_id, grompleaf_id)
                                            VALUES (NEW.user_id, (timezone('utc', now())), 'DeckUpdatedStack', NULL,   NEW.stack_id, NEW.branch_id, NEW.leaf_id, NEW.deck_id,  NULL,       NULL)
                    RETURNING id
                ) INSERT INTO public.received_notification(receiver_id, notification_id)
                                                 (SELECT df.follower_id, (SELECT id FROM notification_id)
                                                  FROM public.deck_follower df
                                                  WHERE df.deck_id = NEW.deck_id
                                                 );
            END IF;
            IF (TG_OP = 'DELETE' OR (TG_OP = 'UPDATE' AND OLD.deck_id <> NEW.deck_id AND old_is_public)) THEN
                WITH notification_id AS (
                    INSERT INTO public.notification(sender_id, time_stamp,              type,          message,     stack_id,     branch_id,     leaf_id,     deck_id, gromplate_id, grompleaf_id)
                                            VALUES (OLD.user_id, (timezone('utc', now())), 'DeckDeletedStack', NULL,   OLD.stack_id, OLD.branch_id, OLD.leaf_id, OLD.deck_id,  NULL,       NULL)
                    RETURNING id
                ) INSERT INTO public.received_notification(receiver_id, notification_id)
                                                 (SELECT df.follower_id, (SELECT id FROM notification_id)
                                                  FROM public.deck_follower df
                                                  WHERE df.deck_id = OLD.deck_id
                                                 );
            END IF;
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_card_afterinsertdeleteupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_card_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF (NEW.tsv_helper IS NOT NULL) THEN
            NEW.tsv = to_tsvector('pg_catalog.english', NEW.tsv_helper);
            NEW.tsv_helper = NULL;
        END IF;
        RETURN NEW;
    END;
$$;


ALTER FUNCTION public.fn_tr_card_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_commeaf_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE commield cf
  SET    latest_id = NEW.id
  WHERE  cf.id = NEW.commield_id;
  IF (NEW.b_weight_tsv_helper IS NOT NULL) THEN
    NEW.tsv =
        setweight(to_tsvector('pg_catalog.english', NEW.field_name), 'A') ||
        setweight(to_tsvector('pg_catalog.english', NEW.b_weight_tsv_helper), 'B');
    NEW.b_weight_tsv_helper = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_tr_commeaf_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_deck_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW.tsv = to_tsvector('pg_catalog.english', NEW.name);
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_deck_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_deckfollower_afterinsertdeleteupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF TG_OP = 'INSERT' THEN
            UPDATE	deck d
            SET     followers = d.followers + 1
            WHERE	d.id = NEW.deck_id;
        ELSIF TG_OP = 'DELETE' THEN
            UPDATE	deck d
            SET     followers = d.followers - 1
            WHERE	d.id = OLD.deck_id;
        ELSIF TG_OP = 'UPDATE' THEN
            RAISE EXCEPTION 'Handle the case when deck_2_follower is updated';
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_deckfollower_afterinsertdeleteupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_grompleaf_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE gromplate g
  SET    latest_id = NEW.id
  WHERE  g.id = NEW.gromplate_id;
  IF (NEW.c_weight_tsv_helper IS NOT NULL) THEN
    NEW.tsv =
        setweight(to_tsvector('pg_catalog.english', NEW.name), 'A') ||
        setweight(to_tsvector('pg_catalog.english', NEW.c_weight_tsv_helper), 'C') ||
        setweight(to_tsvector('pg_catalog.english', NEW.css), 'D');
    NEW.c_weight_tsv_helper = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_tr_grompleaf_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_leaf_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE branch b
  SET    latest_id = NEW.id
  WHERE  b.id = NEW.branch_id;
  IF (NEW.tsv_helper IS NOT NULL) THEN
    NEW.tsv = to_tsvector('pg_catalog.english', NEW.tsv_helper);
    NEW.tsv_helper = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_tr_leaf_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_relationship_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
begin
  NEW.tsv = to_tsvector('pg_catalog.english', NEW.name);
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_relationship_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_tag_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW.tsv = to_tsvector('pg_catalog.english', NEW.name);
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_tag_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_user_afterinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        outer_default_card_setting_id integer NOT NULL := 0;
        outer_default_deck_id         integer NOT NULL := 0;
    BEGIN
        outer_default_card_setting_id := (SELECT id FROM card_setting cs WHERE cs.user_id = 0 LIMIT 1);
        outer_default_deck_id         := (SELECT id FROM deck          d WHERE  d.user_id = 0 LIMIT 1);

        UPDATE card_setting cs
        SET    user_id = NEW.id
        WHERE (cs.id = outer_default_card_setting_id);
        UPDATE padawan p
        SET    default_card_setting_id = outer_default_card_setting_id
        WHERE (p.id = NEW.id);

        UPDATE deck d
        SET    user_id = NEW.id
        WHERE (d.id = outer_default_deck_id);
        UPDATE padawan p
        SET    default_deck_id = outer_default_deck_id
        WHERE (p.id = NEW.id);

        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_user_afterinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_user_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW.tsv = to_tsvector('pg_catalog.simple', NEW.display_name);
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_user_beforeinsertupdate() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE TABLE public.alpha_beta_key (
    id integer NOT NULL,
    key character varying(50) NOT NULL,
    is_used boolean NOT NULL
);


ALTER TABLE public.alpha_beta_key OWNER TO postgres;

ALTER TABLE public.alpha_beta_key ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.alpha_beta_key_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.branch (
    id integer NOT NULL,
    name character varying(64),
    author_id integer NOT NULL,
    stack_id integer NOT NULL,
    latest_id integer NOT NULL,
    users integer NOT NULL,
    is_listed boolean NOT NULL
);


ALTER TABLE public.branch OWNER TO postgres;

ALTER TABLE public.branch ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.branch_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.card (
    id integer NOT NULL,
    user_id integer NOT NULL,
    stack_id integer NOT NULL,
    branch_id integer NOT NULL,
    leaf_id integer NOT NULL,
    index smallint NOT NULL,
    card_state smallint NOT NULL,
    ease_factor_in_permille smallint NOT NULL,
    interval_or_steps_index smallint NOT NULL,
    due timestamp without time zone NOT NULL,
    card_setting_id integer NOT NULL,
    deck_id integer NOT NULL,
    is_lapsed boolean NOT NULL,
    front_personal_field text NOT NULL,
    back_personal_field text NOT NULL,
    tsv_helper text,
    tsv tsvector,
    CONSTRAINT "card. tsv_helper. is null check" CHECK ((tsv_helper IS NULL))
);


ALTER TABLE public.card OWNER TO postgres;

ALTER TABLE public.card ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.card_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE VIEW public.card_is_latest AS
 SELECT a.id,
    a.user_id,
    a.stack_id,
    a.branch_id,
    a.leaf_id,
    a.index,
    a.card_state,
    a.ease_factor_in_permille,
    a.interval_or_steps_index,
    a.due,
    a.card_setting_id,
    a.is_lapsed,
    a.front_personal_field,
    a.back_personal_field,
    a.deck_id,
    (b.latest_id IS NULL) AS is_latest
   FROM (public.card a
     LEFT JOIN public.branch b ON ((b.latest_id = a.leaf_id)));


ALTER TABLE public.card_is_latest OWNER TO postgres;

CREATE TABLE public.card_setting (
    id integer NOT NULL,
    user_id integer NOT NULL,
    name character varying(100) NOT NULL,
    new_cards_steps_in_minutes character varying(100) NOT NULL,
    new_cards_max_per_day smallint NOT NULL,
    new_cards_graduating_interval_in_days smallint NOT NULL,
    new_cards_easy_interval_in_days smallint NOT NULL,
    new_cards_starting_ease_factor_in_permille smallint NOT NULL,
    new_cards_bury_related boolean NOT NULL,
    mature_cards_max_per_day smallint NOT NULL,
    mature_cards_ease_factor_easy_bonus_factor_in_permille smallint NOT NULL,
    mature_cards_interval_factor_in_permille smallint NOT NULL,
    mature_cards_maximum_interval_in_days smallint NOT NULL,
    mature_cards_hard_interval_factor_in_permille smallint NOT NULL,
    mature_cards_bury_related boolean NOT NULL,
    lapsed_cards_steps_in_minutes character varying(100) NOT NULL,
    lapsed_cards_new_interval_factor_in_permille smallint NOT NULL,
    lapsed_cards_minimum_interval_in_days smallint NOT NULL,
    lapsed_cards_leech_threshold smallint NOT NULL,
    show_answer_timer boolean NOT NULL,
    automatically_play_audio boolean NOT NULL,
    replay_question_audio_on_answer boolean NOT NULL
);


ALTER TABLE public.card_setting OWNER TO postgres;

ALTER TABLE public.card_setting ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.card_setting_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.commeaf (
    id integer NOT NULL,
    commield_id integer NOT NULL,
    field_name character varying(200) NOT NULL,
    value text NOT NULL,
    created timestamp without time zone NOT NULL,
    modified timestamp without time zone,
    edit_summary character varying(200) NOT NULL,
    b_weight_tsv_helper text,
    tsv tsvector,
    CONSTRAINT "commeaf. b_weight_tsv_helper. is null check" CHECK ((b_weight_tsv_helper IS NULL))
);


ALTER TABLE public.commeaf OWNER TO postgres;

CREATE TABLE public.commeaf_2_leaf (
    leaf_id integer NOT NULL,
    commeaf_id integer NOT NULL
);


ALTER TABLE public.commeaf_2_leaf OWNER TO postgres;

ALTER TABLE public.commeaf ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.commeaf_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.comment_gromplate (
    id integer NOT NULL,
    gromplate_id integer NOT NULL,
    user_id integer NOT NULL,
    text character varying(500) NOT NULL,
    created timestamp without time zone NOT NULL,
    is_dmca boolean NOT NULL
);


ALTER TABLE public.comment_gromplate OWNER TO postgres;

ALTER TABLE public.comment_gromplate ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.comment_gromplate_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.comment_stack (
    id integer NOT NULL,
    stack_id integer NOT NULL,
    user_id integer NOT NULL,
    text character varying(500) NOT NULL,
    created timestamp without time zone NOT NULL,
    is_dmca boolean NOT NULL
);


ALTER TABLE public.comment_stack OWNER TO postgres;

ALTER TABLE public.comment_stack ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.comment_stack_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.commield (
    id integer NOT NULL,
    author_id integer NOT NULL,
    latest_id integer NOT NULL,
    is_listed boolean NOT NULL
);


ALTER TABLE public.commield OWNER TO postgres;

ALTER TABLE public.commield ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.commield_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.deck (
    id integer NOT NULL,
    user_id integer NOT NULL,
    name character varying(250) NOT NULL,
    is_public boolean NOT NULL,
    source_id integer,
    followers integer NOT NULL,
    tsv tsvector
);


ALTER TABLE public.deck OWNER TO postgres;

CREATE TABLE public.deck_follower (
    deck_id integer NOT NULL,
    follower_id integer NOT NULL
);


ALTER TABLE public.deck_follower OWNER TO postgres;

ALTER TABLE public.deck ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.deck_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.feedback (
    id integer NOT NULL,
    title character varying(50) NOT NULL,
    description character varying(1000) NOT NULL,
    user_id integer NOT NULL,
    created timestamp without time zone NOT NULL,
    parent_id integer,
    priority smallint
);


ALTER TABLE public.feedback OWNER TO postgres;

ALTER TABLE public.feedback ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.feedback_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.file (
    id integer NOT NULL,
    file_name character varying(200) NOT NULL,
    data bytea NOT NULL,
    sha256 bytea NOT NULL
);


ALTER TABLE public.file OWNER TO postgres;

CREATE TABLE public.file_2_leaf (
    leaf_id integer NOT NULL,
    file_id integer NOT NULL
);


ALTER TABLE public.file_2_leaf OWNER TO postgres;

ALTER TABLE public.file ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.file_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.filter (
    id integer NOT NULL,
    name character varying(128) NOT NULL,
    user_id integer NOT NULL,
    query character varying(256) NOT NULL
);


ALTER TABLE public.filter OWNER TO postgres;

ALTER TABLE public.filter ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.filter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.gromplate (
    id integer NOT NULL,
    author_id integer NOT NULL,
    latest_id integer NOT NULL,
    is_listed boolean NOT NULL
);


ALTER TABLE public.gromplate OWNER TO postgres;

ALTER TABLE public.gromplate ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.gromplate_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.grompleaf (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    gromplate_id integer NOT NULL,
    css character varying(4000) NOT NULL,
    created timestamp without time zone NOT NULL,
    modified timestamp without time zone,
    latex_pre character varying(500) NOT NULL,
    latex_post character varying(500) NOT NULL,
    is_dmca boolean NOT NULL,
    templates text NOT NULL,
    type smallint NOT NULL,
    fields character varying(4000) NOT NULL,
    edit_summary character varying(200) NOT NULL,
    anki_id bigint,
    hash bit(512) NOT NULL,
    c_weight_tsv_helper text,
    tsv tsvector,
    CONSTRAINT "grompleaf. c_weight_tsv_helper. is null check" CHECK ((c_weight_tsv_helper IS NULL))
);


ALTER TABLE public.grompleaf OWNER TO postgres;

ALTER TABLE public.grompleaf ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.grompleaf_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.history (
    id bigint NOT NULL,
    card_id integer,
    user_id integer NOT NULL,
    leaf_id integer,
    index smallint NOT NULL,
    score smallint NOT NULL,
    created timestamp without time zone NOT NULL,
    interval_with_unused_steps_index smallint NOT NULL,
    ease_factor_in_permille smallint NOT NULL,
    time_from_seeing_question_to_score_in_seconds_plus32768 smallint NOT NULL
);


ALTER TABLE public.history OWNER TO postgres;

ALTER TABLE public.history ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.history_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.leaf (
    id integer NOT NULL,
    created timestamp without time zone NOT NULL,
    modified timestamp without time zone,
    stack_id integer NOT NULL,
    branch_id integer NOT NULL,
    is_dmca boolean NOT NULL,
    field_values text NOT NULL,
    grompleaf_id integer NOT NULL,
    users integer NOT NULL,
    edit_summary character varying(200) NOT NULL,
    anki_note_id bigint,
    hash bit(512) NOT NULL,
    tsv_helper text,
    tsv tsvector,
    max_index_inclusive smallint NOT NULL,
    CONSTRAINT "leaf. tsv_helper. is null check" CHECK ((tsv_helper IS NULL))
);


ALTER TABLE public.leaf OWNER TO postgres;

ALTER TABLE public.leaf ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.leaf_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.relationship (
    id integer NOT NULL,
    name character varying(250) NOT NULL,
    tsv tsvector
);


ALTER TABLE public.relationship OWNER TO postgres;

CREATE TABLE public.relationship_2_card (
    relationship_id integer NOT NULL,
    user_id integer NOT NULL,
    source_stack_id integer NOT NULL,
    target_stack_id integer NOT NULL,
    source_card_id integer NOT NULL,
    target_card_id integer NOT NULL
);


ALTER TABLE public.relationship_2_card OWNER TO postgres;

CREATE VIEW public.leaf_relationship_count AS
 SELECT sac.leaf_id AS source_leaf_id,
    tac.leaf_id AS target_leaf_id,
    unnest(ARRAY[sac.leaf_id, tac.leaf_id]) AS leaf_id,
    ( SELECT r.name
           FROM public.relationship r
          WHERE (r.id = rac.relationship_id)
         LIMIT 1) AS name,
    count(*) AS count
   FROM ((public.relationship_2_card rac
     JOIN public.card sac ON ((rac.source_card_id = sac.id)))
     JOIN public.card tac ON ((rac.target_card_id = tac.id)))
  WHERE ((sac.card_state <> 3) AND (tac.card_state <> 3))
  GROUP BY sac.leaf_id, tac.leaf_id, rac.relationship_id;


ALTER TABLE public.leaf_relationship_count OWNER TO postgres;

CREATE TABLE public.tag (
    id integer NOT NULL,
    name character varying(250) NOT NULL,
    tsv tsvector
);


ALTER TABLE public.tag OWNER TO postgres;

CREATE TABLE public.tag_2_card (
    tag_id integer NOT NULL,
    user_id integer NOT NULL,
    stack_id integer NOT NULL,
    card_id integer NOT NULL
);


ALTER TABLE public.tag_2_card OWNER TO postgres;

CREATE VIEW public.leaf_tag_count AS
 SELECT i.id AS leaf_id,
    ( SELECT t.name
           FROM public.tag t
          WHERE (t.id = ta.tag_id)
         LIMIT 1) AS name,
    count(*) AS count
   FROM ((public.leaf i
     JOIN public.card cc ON ((cc.leaf_id = i.id)))
     JOIN public.tag_2_card ta ON ((ta.card_id = cc.id)))
  WHERE (cc.card_state <> 3)
  GROUP BY i.id, ta.tag_id;


ALTER TABLE public.leaf_tag_count OWNER TO postgres;

CREATE TABLE public.notification (
    id integer NOT NULL,
    sender_id integer NOT NULL,
    time_stamp timestamp without time zone NOT NULL,
    type public.notification_type NOT NULL,
    message character varying(4000),
    stack_id integer,
    branch_id integer,
    leaf_id integer,
    deck_id integer,
    gromplate_id integer,
    grompleaf_id integer
);


ALTER TABLE public.notification OWNER TO postgres;

ALTER TABLE public.notification ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.notification_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.padawan (
    id integer NOT NULL,
    display_name character varying(32) NOT NULL,
    default_card_setting_id integer NOT NULL,
    default_deck_id integer NOT NULL,
    show_next_review_time boolean NOT NULL,
    show_remaining_card_count boolean NOT NULL,
    mix_new_and_review smallint NOT NULL,
    next_day_starts_at_x_hours_past_midnight smallint NOT NULL,
    learn_ahead_limit_in_minutes smallint NOT NULL,
    timebox_time_limit_in_minutes smallint NOT NULL,
    is_night_mode boolean NOT NULL,
    tsv tsvector
);


ALTER TABLE public.padawan OWNER TO postgres;

CREATE TABLE public.potential_signups (
    id integer NOT NULL,
    email character varying(500) NOT NULL,
    message character varying(1000) NOT NULL,
    one_is_alpha2_beta3_ga smallint NOT NULL,
    time_stamp timestamp without time zone NOT NULL
);


ALTER TABLE public.potential_signups OWNER TO postgres;

ALTER TABLE public.potential_signups ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.potential_signups_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.received_notification (
    receiver_id integer NOT NULL,
    notification_id integer NOT NULL
);


ALTER TABLE public.received_notification OWNER TO postgres;

ALTER TABLE public.relationship ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.relationship_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.stack (
    id integer NOT NULL,
    author_id integer NOT NULL,
    users integer NOT NULL,
    copy_source_id integer,
    default_branch_id integer NOT NULL,
    is_listed boolean NOT NULL
);


ALTER TABLE public.stack OWNER TO postgres;

ALTER TABLE public.stack ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.stack_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE VIEW public.stack_relationship_count AS
 SELECT sac.stack_id AS source_stack_id,
    tac.stack_id AS target_stack_id,
    unnest(ARRAY[sac.stack_id, tac.stack_id]) AS stack_id,
    ( SELECT r.name
           FROM public.relationship r
          WHERE (r.id = rac.relationship_id)
         LIMIT 1) AS name,
    count(*) AS count
   FROM ((public.relationship_2_card rac
     JOIN public.card sac ON ((rac.source_card_id = sac.id)))
     JOIN public.card tac ON ((rac.target_card_id = tac.id)))
  WHERE ((sac.card_state <> 3) AND (tac.card_state <> 3))
  GROUP BY sac.stack_id, tac.stack_id, rac.relationship_id;


ALTER TABLE public.stack_relationship_count OWNER TO postgres;

CREATE VIEW public.stack_tag_count AS
 SELECT s.id AS stack_id,
    ( SELECT t.name
           FROM public.tag t
          WHERE (t.id = ta.tag_id)
         LIMIT 1) AS name,
    count(*) AS count
   FROM ((public.stack s
     JOIN public.card cc ON ((cc.stack_id = s.id)))
     JOIN public.tag_2_card ta ON ((ta.card_id = cc.id)))
  WHERE (cc.card_state <> 3)
  GROUP BY s.id, ta.tag_id;


ALTER TABLE public.stack_tag_count OWNER TO postgres;

CREATE TABLE public.tag_2_user_2_grompleaf (
    user_id integer NOT NULL,
    grompleaf_id integer NOT NULL,
    default_tag_id integer NOT NULL
);


ALTER TABLE public.tag_2_user_2_grompleaf OWNER TO postgres;

ALTER TABLE public.tag ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.tag_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.user_2_grompleaf (
    user_id integer NOT NULL,
    grompleaf_id integer NOT NULL,
    default_card_setting_id integer NOT NULL
);


ALTER TABLE public.user_2_grompleaf OWNER TO postgres;

ALTER TABLE public.padawan ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.user_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public.vote_2_comment_gromplate (
    comment_gromplate_id integer NOT NULL,
    user_id integer NOT NULL
);


ALTER TABLE public.vote_2_comment_gromplate OWNER TO postgres;

CREATE TABLE public.vote_2_comment_stack (
    comment_stack_id integer NOT NULL,
    user_id integer NOT NULL
);


ALTER TABLE public.vote_2_comment_stack OWNER TO postgres;

CREATE TABLE public.vote_2_feedback (
    feedback_id integer NOT NULL,
    user_id integer NOT NULL
);


ALTER TABLE public.vote_2_feedback OWNER TO postgres;







INSERT INTO public.card_setting (id, user_id, name, new_cards_steps_in_minutes, new_cards_max_per_day, new_cards_graduating_interval_in_days, new_cards_easy_interval_in_days, new_cards_starting_ease_factor_in_permille, new_cards_bury_related, mature_cards_max_per_day, mature_cards_ease_factor_easy_bonus_factor_in_permille, mature_cards_interval_factor_in_permille, mature_cards_maximum_interval_in_days, mature_cards_hard_interval_factor_in_permille, mature_cards_bury_related, lapsed_cards_steps_in_minutes, lapsed_cards_new_interval_factor_in_permille, lapsed_cards_minimum_interval_in_days, lapsed_cards_leech_threshold, show_answer_timer, automatically_play_audio, replay_question_audio_on_answer) VALUES (1, 1, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public.card_setting (id, user_id, name, new_cards_steps_in_minutes, new_cards_max_per_day, new_cards_graduating_interval_in_days, new_cards_easy_interval_in_days, new_cards_starting_ease_factor_in_permille, new_cards_bury_related, mature_cards_max_per_day, mature_cards_ease_factor_easy_bonus_factor_in_permille, mature_cards_interval_factor_in_permille, mature_cards_maximum_interval_in_days, mature_cards_hard_interval_factor_in_permille, mature_cards_bury_related, lapsed_cards_steps_in_minutes, lapsed_cards_new_interval_factor_in_permille, lapsed_cards_minimum_interval_in_days, lapsed_cards_leech_threshold, show_answer_timer, automatically_play_audio, replay_question_audio_on_answer) VALUES (2, 2, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public.card_setting (id, user_id, name, new_cards_steps_in_minutes, new_cards_max_per_day, new_cards_graduating_interval_in_days, new_cards_easy_interval_in_days, new_cards_starting_ease_factor_in_permille, new_cards_bury_related, mature_cards_max_per_day, mature_cards_ease_factor_easy_bonus_factor_in_permille, mature_cards_interval_factor_in_permille, mature_cards_maximum_interval_in_days, mature_cards_hard_interval_factor_in_permille, mature_cards_bury_related, lapsed_cards_steps_in_minutes, lapsed_cards_new_interval_factor_in_permille, lapsed_cards_minimum_interval_in_days, lapsed_cards_leech_threshold, show_answer_timer, automatically_play_audio, replay_question_audio_on_answer) VALUES (3, 3, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);












INSERT INTO public.deck (id, user_id, name, is_public, source_id, followers, tsv) VALUES (1, 1, 'Default Deck', false, NULL, 0, NULL);
INSERT INTO public.deck (id, user_id, name, is_public, source_id, followers, tsv) VALUES (2, 2, 'Default Deck', false, NULL, 0, NULL);
INSERT INTO public.deck (id, user_id, name, is_public, source_id, followers, tsv) VALUES (3, 3, 'Default Deck', false, NULL, 0, NULL);












INSERT INTO public.gromplate (id, author_id, latest_id, is_listed) VALUES (1, 2, 1001, true);
INSERT INTO public.gromplate (id, author_id, latest_id, is_listed) VALUES (2, 2, 1002, true);
INSERT INTO public.gromplate (id, author_id, latest_id, is_listed) VALUES (3, 2, 1003, true);
INSERT INTO public.gromplate (id, author_id, latest_id, is_listed) VALUES (4, 2, 1006, true);
INSERT INTO public.gromplate (id, author_id, latest_id, is_listed) VALUES (5, 2, 1007, true);


INSERT INTO public.grompleaf (id, name, gromplate_id, css, created, modified, latex_pre, latex_post, is_dmca, templates, type, fields, edit_summary, anki_id, hash, c_weight_tsv_helper, tsv) VALUES (1001, 'Basic', 1, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', '2019-04-08 02:14:29.581', '2019-06-16 00:53:30', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, 'Card 1{{Front}}{{FrontSide}}

<hr id=answer>

{{Back}}', 0, 'FrontFalseFalseBackFalseFalse', 'Imported from Anki', 1554689669581, B'10011010111111101110110011101011110011110101011100000110000000101000000110110000011000001000100110100011011101011110011110001110011001110110101001111110010011110110110110110111110001000111001010011101110110111100100000011101011000001100010111110101110111011100001001110010101100011001110100100010101011000011010110110001000111100101100101000101110010110011110001011100000000001011011001101000000110111100111010010100100100110100010101000000000000000010111001010001001010000011000111001011111001010010110111001100', NULL, '''1'':5C ''20px'':17 ''align'':20 ''arial'':13 ''back'':3C,8C ''background'':25 ''background-color'':24 ''basic'':1A ''black'':23 ''card'':4C,9 ''center'':21 ''color'':22,26 ''famili'':12 ''font'':11,15 ''font-famili'':10 ''font-siz'':14 ''front'':2C,6C ''frontsid'':7C ''size'':16 ''text'':19 ''text-align'':18 ''white'':27');
INSERT INTO public.grompleaf (id, name, gromplate_id, css, created, modified, latex_pre, latex_post, is_dmca, templates, type, fields, edit_summary, anki_id, hash, c_weight_tsv_helper, tsv) VALUES (1002, 'Basic (optional reversed card)', 2, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', '2019-04-08 02:14:29.577', '2019-06-16 00:51:28', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, 'Card 1{{Front}}{{FrontSide}}

<hr id=answer>

{{Back}}Card 2{{#Add Reverse}}{{Back}}{{/Add Reverse}}{{FrontSide}}

<hr id=answer>

{{Front}}', 0, 'FrontFalseFalseBackFalseFalseAdd ReverseFalseFalse', 'Imported from Anki', 1554689669572, B'10011100101011110010101010111010100011010000110011111010001011100001101110011001100000010001110001101110101110011010011010011001100010100111010001101010000001001101010110010010111111000100011011111001001101111011100001011000111101000001001011001100100111110111011001110110011111110010011110011100001100100001000010010101111101000010001010101010001100000110001011011110110101010011000011101010100111010101011001101101100000011110011100010010101101000000001100111111011111111011000011010000001000011111111010110110', NULL, '''/add'':19C ''1'':10C ''2'':15C ''20px'':31 ''add'':7C,16C ''align'':34 ''arial'':27 ''back'':6C,13C,18C ''background'':39 ''background-color'':38 ''basic'':1A ''black'':37 ''card'':4A,9C,14C,23 ''center'':35 ''color'':36,40 ''famili'':26 ''font'':25,29 ''font-famili'':24 ''font-siz'':28 ''front'':5C,11C,22C ''frontsid'':12C,21C ''option'':2A ''revers'':3A,8C,17C,20C ''size'':30 ''text'':33 ''text-align'':32 ''white'':41');
INSERT INTO public.grompleaf (id, name, gromplate_id, css, created, modified, latex_pre, latex_post, is_dmca, templates, type, fields, edit_summary, anki_id, hash, c_weight_tsv_helper, tsv) VALUES (1003, 'Basic (and reversed card)', 3, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', '2019-04-08 02:14:29.577', '2019-06-16 00:51:28', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, 'Card 1{{Front}}{{FrontSide}}

<hr id=answer>

{{Back}}Card 2{{Back}}{{FrontSide}}

<hr id=answer>

{{Front}}', 0, 'FrontFalseFalseBackFalseFalse', 'Imported from Anki', 1554689669577, B'11010101100110100001101111011111100000111101010111000101110011111000111000101111000101001101101101110001010110100000110110010101100100110101111000000011001111000101011000010010110010111010000000111110111101110110100011010010101000001100011010010001010001111000001100101010101101001010011000000110000100010110000011101101010111111110000101001111000010101110010001101011111010101011111111001010110010001100010011010100010000100011010100010101100000001011101101101010001001100011000011100011101011010000110101100001', NULL, '''1'':8C ''2'':13C ''20px'':25 ''align'':28 ''arial'':21 ''back'':6C,11C,14C ''background'':33 ''background-color'':32 ''basic'':1A ''black'':31 ''card'':4A,7C,12C,17 ''center'':29 ''color'':30,34 ''famili'':20 ''font'':19,23 ''font-famili'':18 ''font-siz'':22 ''front'':5C,9C,16C ''frontsid'':10C,15C ''revers'':3A ''size'':24 ''text'':27 ''text-align'':26 ''white'':35');
INSERT INTO public.grompleaf (id, name, gromplate_id, css, created, modified, latex_pre, latex_post, is_dmca, templates, type, fields, edit_summary, anki_id, hash, c_weight_tsv_helper, tsv) VALUES (1004, 'Basic (type in the answer)', 4, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', '2019-04-08 02:14:29.571', '2019-06-16 00:51:46', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, 'Card 1{{Front}}
{{type:Back}}{{FrontSide}}

<hr id=answer>

{{Back}}', 0, 'FrontFalseFalseBackFalseFalse', 'Imported from Anki', 1554689669571, B'10011010000110101110010010000001101101100010101011100100101001110010110110001010010101001110111001000111111100001011010010000110011001111111100000011110110101110000111100111010011110101001101100000011011110011110000110011111110001011011010101011001101000110011110110110010100110000110001110100101001010110111110001100011001110010011100111111011110110000010001000101011110000100010001000010010010001101111000000011000111110110001011001100100101011010100111110011000000000000111001110010010100111111001111100110000', NULL, '''1'':9C ''20px'':23 ''align'':26 ''answer'':5A ''arial'':19 ''back'':7C,12C,14C ''background'':31 ''background-color'':30 ''basic'':1A ''black'':29 ''card'':8C,15 ''center'':27 ''color'':28,32 ''famili'':18 ''font'':17,21 ''font-famili'':16 ''font-siz'':20 ''front'':6C,10C ''frontsid'':13C ''size'':22 ''text'':25 ''text-align'':24 ''type'':2A,11C ''white'':33');
INSERT INTO public.grompleaf (id, name, gromplate_id, css, created, modified, latex_pre, latex_post, is_dmca, templates, type, fields, edit_summary, anki_id, hash, c_weight_tsv_helper, tsv) VALUES (1005, 'Cloze', 5, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

.cloze {
 font-weight: bold;
 color: blue;
}
.nightMode .cloze {
 color: lightblue;
}', '2019-04-08 02:14:29.57', '2019-06-16 00:51:55', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, 'Cloze{{cloze:Text}}{{cloze:Text}}<br>
{{Extra}}', 1, 'TextFalseFalseExtraFalseFalse', 'Imported from Anki', 1554689669570, B'00100010000000111110010011011111110110111111010110100000101001001011100011011010101101101001001111011110100011110010110010110011011100001001010000111001100110100111101111000100010100011101101011111100111100101100101011100101110110110110000100000111111000000110010100101101011011111101110101100000100010111000001010101100010111010111011100111000001001001110101000001100110010110001011000010110001001000110111110100010001111111111100011011010110010000011010100011011110101100010100100100110001110100011011101101101', NULL, '''20px'':18 ''align'':21 ''arial'':14 ''background'':26 ''background-color'':25 ''black'':24 ''blue'':35 ''bold'':33 ''card'':10 ''center'':22 ''cloze'':1A,4C,5C,7C,29,37 ''color'':23,27,34,38 ''extra'':3C,9C ''famili'':13 ''font'':12,16,31 ''font-famili'':11 ''font-siz'':15 ''font-weight'':30 ''lightblu'':39 ''nightmod'':36 ''size'':17 ''text'':2C,6C,8C,20 ''text-align'':19 ''weight'':32 ''white'':28');
INSERT INTO public.grompleaf (id, name, gromplate_id, css, created, modified, latex_pre, latex_post, is_dmca, templates, type, fields, edit_summary, anki_id, hash, c_weight_tsv_helper, tsv) VALUES (1006, 'Basic (type in the answer)', 4, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', '2020-04-23 19:40:46.82', '2020-04-23 19:40:46', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, 'Card 1{{Front}}

{{type:Back}}{{Front}}

<hr id=answer>

{{type:Back}}', 0, 'FrontFalseFalseBackFalseFalse', 'Answer uses {{Front}} instead of {{FrontSide}} and {{type:Back}} instead of {{Back}} as of Anki v2.1.15', 1587486094455, B'11111101101111111011100111011000100110111111000110101111101000001011000100011111011000111110110111001111100111000000111111101000100111010011011000100010101110110010110000000100010110111011001111111010011110110101111010000110011110010101110000101111110000001111101000010101010101000001100011010011110011111110011010001010100001011110100000110010011000100011110000111100101100110011000011111000101111111101100110111010011000001001001100001010100011010101000111110011000011001010101001000100110111001011101010001010', NULL, '''20px'':22 ''align'':25 ''answer'':5A ''arial'':18 ''back'':7C,10C,13C ''background'':30 ''background-color'':29 ''basic'':1A ''black'':28 ''card'':14 ''center'':26 ''color'':27,31 ''famili'':17 ''font'':16,20 ''font-famili'':15 ''font-siz'':19 ''front'':6C,8C,11C ''size'':21 ''text'':24 ''text-align'':23 ''type'':2A,9C,12C ''white'':32');
INSERT INTO public.grompleaf (id, name, gromplate_id, css, created, modified, latex_pre, latex_post, is_dmca, templates, type, fields, edit_summary, anki_id, hash, c_weight_tsv_helper, tsv) VALUES (1007, 'Cloze', 5, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

.cloze {
 font-weight: bold;
 color: blue;
}
.nightMode .cloze {
 color: lightblue;
}', '2020-04-23 19:40:46.82', '2020-04-23 19:40:46', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, 'Cloze{{cloze:Text}}{{cloze:Text}}', 1, 'TextFalseFalse', '"Extra" field removed in Anki v2.1.22', 1587670846820, B'11001001010011011111111001100111011000001111110010001110000001011100101101000101001110101111111111101011011100100010111010001111110111101011011011111001111100000011001110001001101101000110001011011100011011001110111100010100100111000011010010010011110111110000111100000111111000110100101110111000011100000000000010001101000101111011000000101101011000011000100010001010100110011011000101010011111010011001001010010000100111111101010100000011110101001011001011101100101011111100100001101111111001111110100111001000', NULL, '''20px'':15 ''align'':18 ''arial'':11 ''background'':23 ''background-color'':22 ''black'':21 ''blue'':32 ''bold'':30 ''card'':7 ''center'':19 ''cloze'':1A,3C,5C,26,34 ''color'':20,24,31,35 ''famili'':10 ''font'':9,13,28 ''font-famili'':8 ''font-siz'':12 ''font-weight'':27 ''lightblu'':36 ''nightmod'':33 ''size'':14 ''text'':2C,4C,6C,17 ''text-align'':16 ''weight'':29 ''white'':25');








INSERT INTO public.padawan (id, display_name, default_card_setting_id, default_deck_id, show_next_review_time, show_remaining_card_count, mix_new_and_review, next_day_starts_at_x_hours_past_midnight, learn_ahead_limit_in_minutes, timebox_time_limit_in_minutes, is_night_mode, tsv) VALUES (1, 'Admin', 1, 1, true, true, 0, 4, 20, 0, false, '''admin'':1');
INSERT INTO public.padawan (id, display_name, default_card_setting_id, default_deck_id, show_next_review_time, show_remaining_card_count, mix_new_and_review, next_day_starts_at_x_hours_past_midnight, learn_ahead_limit_in_minutes, timebox_time_limit_in_minutes, is_night_mode, tsv) VALUES (2, 'The Collective', 2, 2, true, true, 0, 4, 20, 0, false, '''collective'':2 ''the'':1');
INSERT INTO public.padawan (id, display_name, default_card_setting_id, default_deck_id, show_next_review_time, show_remaining_card_count, mix_new_and_review, next_day_starts_at_x_hours_past_midnight, learn_ahead_limit_in_minutes, timebox_time_limit_in_minutes, is_night_mode, tsv) VALUES (3, 'RoboTurtle', 3, 3, true, true, 0, 4, 20, 0, false, '''roboturtle'':1');


















INSERT INTO public.user_2_grompleaf (user_id, grompleaf_id, default_card_setting_id) VALUES (3, 1001, 3);
INSERT INTO public.user_2_grompleaf (user_id, grompleaf_id, default_card_setting_id) VALUES (3, 1002, 3);
INSERT INTO public.user_2_grompleaf (user_id, grompleaf_id, default_card_setting_id) VALUES (3, 1003, 3);
INSERT INTO public.user_2_grompleaf (user_id, grompleaf_id, default_card_setting_id) VALUES (3, 1006, 3);
INSERT INTO public.user_2_grompleaf (user_id, grompleaf_id, default_card_setting_id) VALUES (3, 1005, 3);








SELECT pg_catalog.setval('public.alpha_beta_key_id_seq', 1, false);


SELECT pg_catalog.setval('public.branch_id_seq', 1, false);


SELECT pg_catalog.setval('public.card_id_seq', 1, false);


SELECT pg_catalog.setval('public.card_setting_id_seq', 4, false);


SELECT pg_catalog.setval('public.commeaf_id_seq', 1001, false);


SELECT pg_catalog.setval('public.comment_gromplate_id_seq', 1, false);


SELECT pg_catalog.setval('public.comment_stack_id_seq', 1, false);


SELECT pg_catalog.setval('public.commield_id_seq', 1, false);


SELECT pg_catalog.setval('public.deck_id_seq', 4, false);


SELECT pg_catalog.setval('public.feedback_id_seq', 1, false);


SELECT pg_catalog.setval('public.file_id_seq', 1, false);


SELECT pg_catalog.setval('public.filter_id_seq', 1, false);


SELECT pg_catalog.setval('public.gromplate_id_seq', 6, false);


SELECT pg_catalog.setval('public.grompleaf_id_seq', 1008, false);


SELECT pg_catalog.setval('public.history_id_seq', 1, false);


SELECT pg_catalog.setval('public.leaf_id_seq', 1001, false);


SELECT pg_catalog.setval('public.notification_id_seq', 1, false);


SELECT pg_catalog.setval('public.potential_signups_id_seq', 1, false);


SELECT pg_catalog.setval('public.relationship_id_seq', 1, false);


SELECT pg_catalog.setval('public.stack_id_seq', 1, false);


SELECT pg_catalog.setval('public.tag_id_seq', 1, false);


SELECT pg_catalog.setval('public.user_id_seq', 4, false);


ALTER TABLE ONLY public.alpha_beta_key
    ADD CONSTRAINT alpha_beta_key_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.branch
    ADD CONSTRAINT branch_id_stack_id_key UNIQUE (id, stack_id);


ALTER TABLE ONLY public.branch
    ADD CONSTRAINT branch_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT card_id_stack_id_user_id_key UNIQUE (id, stack_id, user_id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT card_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.card_setting
    ADD CONSTRAINT card_setting_id_user_id_key UNIQUE (id, user_id);


ALTER TABLE ONLY public.card_setting
    ADD CONSTRAINT card_setting_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.commeaf_2_leaf
    ADD CONSTRAINT commeaf_2_leaf_pkey PRIMARY KEY (commeaf_id, leaf_id);


ALTER TABLE ONLY public.commeaf
    ADD CONSTRAINT commeaf_id_commield_id_key UNIQUE (id, commield_id);


ALTER TABLE ONLY public.commeaf
    ADD CONSTRAINT commeaf_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.comment_gromplate
    ADD CONSTRAINT comment_gromplate_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.comment_stack
    ADD CONSTRAINT comment_stack_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.commield
    ADD CONSTRAINT commield_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.deck_follower
    ADD CONSTRAINT deck_follower_pkey PRIMARY KEY (deck_id, follower_id);


ALTER TABLE ONLY public.deck
    ADD CONSTRAINT deck_id_user_id_key UNIQUE (id, user_id);


ALTER TABLE ONLY public.deck
    ADD CONSTRAINT deck_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.feedback
    ADD CONSTRAINT feedback_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.file_2_leaf
    ADD CONSTRAINT file_2_leaf_pkey PRIMARY KEY (leaf_id, file_id);


ALTER TABLE ONLY public.file
    ADD CONSTRAINT file_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.filter
    ADD CONSTRAINT filter_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.gromplate
    ADD CONSTRAINT gromplate_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.grompleaf
    ADD CONSTRAINT grompleaf_id_gromplate_id_key UNIQUE (id, gromplate_id);


ALTER TABLE ONLY public.grompleaf
    ADD CONSTRAINT grompleaf_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.history
    ADD CONSTRAINT history_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.leaf
    ADD CONSTRAINT leaf_id_branch_id_key UNIQUE (id, branch_id);


ALTER TABLE ONLY public.leaf
    ADD CONSTRAINT leaf_id_stack_id_key UNIQUE (id, stack_id);


ALTER TABLE ONLY public.leaf
    ADD CONSTRAINT leaf_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT notification_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.padawan
    ADD CONSTRAINT padawan_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.potential_signups
    ADD CONSTRAINT potential_signups_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.received_notification
    ADD CONSTRAINT received_notification_pkey PRIMARY KEY (notification_id, receiver_id);


ALTER TABLE ONLY public.relationship_2_card
    ADD CONSTRAINT relationship_2_card_pkey PRIMARY KEY (source_stack_id, target_stack_id, relationship_id, user_id);


ALTER TABLE ONLY public.relationship
    ADD CONSTRAINT relationship_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.stack
    ADD CONSTRAINT stack_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.tag_2_card
    ADD CONSTRAINT tag_2_card_pkey PRIMARY KEY (stack_id, tag_id, user_id);


ALTER TABLE ONLY public.tag_2_user_2_grompleaf
    ADD CONSTRAINT tag_2_user_2_grompleaf_pkey PRIMARY KEY (default_tag_id, grompleaf_id, user_id);


ALTER TABLE ONLY public.tag
    ADD CONSTRAINT tag_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.user_2_grompleaf
    ADD CONSTRAINT user_2_grompleaf_pkey PRIMARY KEY (grompleaf_id, user_id);


ALTER TABLE ONLY public.vote_2_comment_gromplate
    ADD CONSTRAINT vote_2_comment_gromplate_pkey PRIMARY KEY (comment_gromplate_id, user_id);


ALTER TABLE ONLY public.vote_2_comment_stack
    ADD CONSTRAINT vote_2_comment_stack_pkey PRIMARY KEY (comment_stack_id, user_id);


ALTER TABLE ONLY public.vote_2_feedback
    ADD CONSTRAINT vote_2_feedback_pkey PRIMARY KEY (feedback_id, user_id);


CREATE UNIQUE INDEX "alpha_beta_key. key. uq idx" ON public.alpha_beta_key USING btree (key);


CREATE UNIQUE INDEX "branch. stack_id, upper(name). uq idx" ON public.branch USING btree (stack_id, upper((name)::text));


CREATE INDEX "card. card_setting_id. idx" ON public.card USING btree (card_setting_id);


CREATE INDEX "card. card_state. idx" ON public.card USING btree (card_state);


CREATE INDEX "card. leaf_id. idx" ON public.card USING btree (leaf_id);


CREATE INDEX "card. tsv. idx" ON public.card USING gin (tsv);


CREATE INDEX "card. user_id, branch_id. idx" ON public.card USING btree (user_id, branch_id);


CREATE UNIQUE INDEX "card. user_id, leaf_id, index. uq idx" ON public.card USING btree (user_id, leaf_id, index);


CREATE INDEX "card. user_id, stack_id. idx" ON public.card USING btree (user_id, stack_id);


CREATE INDEX "card. user_id. idx" ON public.card USING btree (user_id);


CREATE INDEX "card_setting. user_id. idx" ON public.card_setting USING btree (user_id);


CREATE INDEX "commeaf. commield_id. idx" ON public.commeaf USING btree (commield_id);


CREATE INDEX "commeaf. tsv. idx" ON public.commeaf USING gin (tsv);


CREATE INDEX "commeaf_2_leaf. leaf_id. idx" ON public.commeaf_2_leaf USING btree (leaf_id);


CREATE INDEX "comment_gromplate. gromplate_id. idx" ON public.comment_gromplate USING btree (gromplate_id);


CREATE INDEX "comment_gromplate. user_id. idx" ON public.comment_gromplate USING btree (user_id);


CREATE INDEX "comment_stack. stack_id. idx" ON public.comment_stack USING btree (stack_id);


CREATE INDEX "comment_stack. user_id. idx" ON public.comment_stack USING btree (user_id);


CREATE INDEX "commield. author_id. idx" ON public.commield USING btree (author_id);


CREATE INDEX "deck. tsv. idx" ON public.deck USING gin (tsv);


CREATE UNIQUE INDEX "deck. user_id, upper(name). uq idx" ON public.deck USING btree (user_id, upper((name)::text));


CREATE INDEX "feedback. parent_id. idx" ON public.feedback USING btree (parent_id);


CREATE INDEX "feedback. user_id. idx" ON public.feedback USING btree (user_id);


CREATE UNIQUE INDEX "file. sha256. uq idx" ON public.file USING btree (sha256);


CREATE INDEX "file_2_leaf. file_id. idx" ON public.file_2_leaf USING btree (file_id);


CREATE INDEX "filter. user_id. idx" ON public.filter USING btree (user_id);


CREATE INDEX "gromplate. author_id. idx" ON public.gromplate USING btree (author_id);


CREATE INDEX "grompleaf. gromplate_id. idx" ON public.grompleaf USING btree (gromplate_id);


CREATE INDEX "grompleaf. hash. idx" ON public.grompleaf USING btree (hash);


CREATE INDEX "grompleaf. tsv. idx" ON public.grompleaf USING gin (tsv);


CREATE INDEX "history. card_id. idx" ON public.history USING btree (card_id);


CREATE INDEX "leaf. branch_id. idx" ON public.leaf USING btree (branch_id);


CREATE INDEX "leaf. grompleaf_id. idx" ON public.leaf USING btree (grompleaf_id);


CREATE INDEX "leaf. hash. idx" ON public.leaf USING btree (hash);


CREATE INDEX "leaf. tsv. idx" ON public.leaf USING gin (tsv);


CREATE INDEX "relationship. tsv. idx" ON public.relationship USING gin (tsv);


CREATE UNIQUE INDEX "relationship. upper(name). uq idx" ON public.relationship USING btree (upper((name)::text));


CREATE INDEX "relationship_2_card. relationship_id. idx" ON public.relationship_2_card USING btree (relationship_id);


CREATE INDEX "relationship_2_card. source_card_id. idx" ON public.relationship_2_card USING btree (source_card_id);


CREATE INDEX "relationship_2_card. target_card_id. idx" ON public.relationship_2_card USING btree (target_card_id);


CREATE INDEX "stack. author_id. idx" ON public.stack USING btree (author_id);


CREATE INDEX "tag. tsv. idx" ON public.tag USING gin (tsv);


CREATE UNIQUE INDEX "tag. upper(name). uq idx" ON public.tag USING btree (upper((name)::text));


CREATE INDEX "tag_2_card. card_id. idx" ON public.tag_2_card USING btree (card_id);


CREATE UNIQUE INDEX "tag_2_card. tag_id, stack_id, user_id. uq idx" ON public.tag_2_card USING btree (tag_id, stack_id, user_id);


CREATE INDEX "tag_2_user_2_grompleaf. default_tag_id. idx" ON public.tag_2_user_2_grompleaf USING btree (default_tag_id);


CREATE INDEX "user. tsv. idx" ON public.padawan USING gin (tsv);


CREATE INDEX "user_2_grompleaf. default_card_setting_id. idx" ON public.user_2_grompleaf USING btree (default_card_setting_id);


CREATE INDEX "user_2_grompleaf. grompleaf_id. idx" ON public.user_2_grompleaf USING btree (grompleaf_id);


CREATE INDEX "vote_2_comment_gromplate. user_id. idx" ON public.vote_2_comment_gromplate USING btree (user_id);


CREATE INDEX "vote_2_comment_stack. user_id. idx" ON public.vote_2_comment_stack USING btree (user_id);


CREATE INDEX "vote_2_feedback. user_id. idx" ON public.vote_2_feedback USING btree (user_id);


CREATE CONSTRAINT TRIGGER ctr_branch_insertupdate AFTER INSERT OR UPDATE ON public.branch DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.fn_ctr_branch_insertupdate();


CREATE CONSTRAINT TRIGGER ctr_card_insertupdate AFTER INSERT OR UPDATE ON public.card DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.fn_ctr_card_insertupdate();


CREATE TRIGGER tr_branch_afterinsertupdate AFTER INSERT OR UPDATE ON public.branch FOR EACH ROW EXECUTE FUNCTION public.fn_tr_branch_afterinsertupdate();


CREATE TRIGGER tr_card_afterinsertdeleteupdate AFTER INSERT OR DELETE OR UPDATE ON public.card FOR EACH ROW EXECUTE FUNCTION public.fn_tr_card_afterinsertdeleteupdate();


CREATE TRIGGER tr_card_beforeinsertupdate BEFORE INSERT OR UPDATE ON public.card FOR EACH ROW EXECUTE FUNCTION public.fn_tr_card_beforeinsertupdate();


CREATE TRIGGER tr_commeaf_beforeinsert BEFORE INSERT ON public.commeaf FOR EACH ROW EXECUTE FUNCTION public.fn_tr_commeaf_beforeinsert();


CREATE TRIGGER tr_deck_beforeinsertupdate BEFORE INSERT OR UPDATE ON public.deck FOR EACH ROW EXECUTE FUNCTION public.fn_tr_deck_beforeinsertupdate();


CREATE TRIGGER tr_deckfollower_afterinsertdeleteupdate AFTER INSERT OR DELETE OR UPDATE ON public.deck_follower FOR EACH ROW EXECUTE FUNCTION public.fn_tr_deckfollower_afterinsertdeleteupdate();


CREATE TRIGGER tr_grompleaf_beforeinsert BEFORE INSERT ON public.grompleaf FOR EACH ROW EXECUTE FUNCTION public.fn_tr_grompleaf_beforeinsert();


CREATE TRIGGER tr_leaf_beforeinsert BEFORE INSERT ON public.leaf FOR EACH ROW EXECUTE FUNCTION public.fn_tr_leaf_beforeinsert();


CREATE TRIGGER tr_relationship_beforeinsertupdate BEFORE INSERT OR UPDATE ON public.relationship FOR EACH ROW EXECUTE FUNCTION public.fn_tr_relationship_beforeinsertupdate();


CREATE TRIGGER tr_tag_beforeinsertupdate BEFORE INSERT OR UPDATE ON public.tag FOR EACH ROW EXECUTE FUNCTION public.fn_tr_tag_beforeinsertupdate();


CREATE TRIGGER tr_user_afterinsert AFTER INSERT ON public.padawan FOR EACH ROW EXECUTE FUNCTION public.fn_tr_user_afterinsert();


CREATE TRIGGER tr_user_beforeinsertupdate BEFORE INSERT OR UPDATE ON public.padawan FOR EACH ROW EXECUTE FUNCTION public.fn_tr_user_beforeinsertupdate();


ALTER TABLE ONLY public.branch
    ADD CONSTRAINT "branch to leaf. latest_id, id. fkey" FOREIGN KEY (latest_id, id) REFERENCES public.leaf(id, branch_id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.branch
    ADD CONSTRAINT "branch to stack. stack_id. fkey" FOREIGN KEY (stack_id) REFERENCES public.stack(id);


ALTER TABLE ONLY public.branch
    ADD CONSTRAINT "branch to user. author_id. fkey" FOREIGN KEY (author_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to branch. branch_id. fkey" FOREIGN KEY (branch_id) REFERENCES public.branch(id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to branch. stack_id, branch_id. fkey" FOREIGN KEY (stack_id, branch_id) REFERENCES public.branch(stack_id, id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to card_setting. card_setting_id. fkey" FOREIGN KEY (card_setting_id) REFERENCES public.card_setting(id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to deck. deck_id. fkey" FOREIGN KEY (deck_id) REFERENCES public.deck(id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to leaf. branch_id, leaf_id. fkey" FOREIGN KEY (branch_id, leaf_id) REFERENCES public.leaf(branch_id, id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to leaf. leaf_id. fkey" FOREIGN KEY (leaf_id) REFERENCES public.leaf(id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to stack. stack_id. fkey" FOREIGN KEY (stack_id) REFERENCES public.stack(id);


ALTER TABLE ONLY public.card
    ADD CONSTRAINT "card to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.card_setting
    ADD CONSTRAINT "card_setting to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.commeaf
    ADD CONSTRAINT "commeaf to commield. commield_id. fkey" FOREIGN KEY (commield_id) REFERENCES public.commield(id);


ALTER TABLE ONLY public.commeaf_2_leaf
    ADD CONSTRAINT "commeaf_2_leaf to commeaf. commeaf_id. fkey" FOREIGN KEY (commeaf_id) REFERENCES public.commeaf(id);


ALTER TABLE ONLY public.commeaf_2_leaf
    ADD CONSTRAINT "commeaf_2_leaf to leaf. leaf_id. fkey" FOREIGN KEY (leaf_id) REFERENCES public.leaf(id);


ALTER TABLE ONLY public.comment_gromplate
    ADD CONSTRAINT "comment_gromplate to gromplate. gromplate_id. fkey" FOREIGN KEY (gromplate_id) REFERENCES public.gromplate(id);


ALTER TABLE ONLY public.comment_gromplate
    ADD CONSTRAINT "comment_gromplate to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.comment_stack
    ADD CONSTRAINT "comment_stack to stack. stack_id. fkey" FOREIGN KEY (stack_id) REFERENCES public.stack(id);


ALTER TABLE ONLY public.comment_stack
    ADD CONSTRAINT "comment_stack to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.commield
    ADD CONSTRAINT "commield to commeaf. latest_id, id. fkey" FOREIGN KEY (latest_id, id) REFERENCES public.commeaf(id, commield_id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.commield
    ADD CONSTRAINT "commield to user. author_id. fkey" FOREIGN KEY (author_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.deck
    ADD CONSTRAINT "deck to deck. source_id. fkey" FOREIGN KEY (source_id) REFERENCES public.deck(id) ON DELETE SET NULL;


ALTER TABLE ONLY public.deck
    ADD CONSTRAINT "deck to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.deck_follower
    ADD CONSTRAINT "deck_follower to deck. deck_id. fkey" FOREIGN KEY (deck_id) REFERENCES public.deck(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.deck_follower
    ADD CONSTRAINT "deck_follower to user. follower_id. fkey" FOREIGN KEY (follower_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.feedback
    ADD CONSTRAINT "feedback to feedback. parent_id. fkey" FOREIGN KEY (parent_id) REFERENCES public.feedback(id);


ALTER TABLE ONLY public.feedback
    ADD CONSTRAINT "feedback to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.file_2_leaf
    ADD CONSTRAINT "file_2_leaf to file. file_id. fkey" FOREIGN KEY (file_id) REFERENCES public.file(id);


ALTER TABLE ONLY public.file_2_leaf
    ADD CONSTRAINT "file_2_leaf to leaf. leaf_id. fkey" FOREIGN KEY (leaf_id) REFERENCES public.leaf(id);


ALTER TABLE ONLY public.filter
    ADD CONSTRAINT "filter to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.gromplate
    ADD CONSTRAINT "gromplate to grompleaf. latest_id, id. fkey" FOREIGN KEY (latest_id, id) REFERENCES public.grompleaf(id, gromplate_id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.gromplate
    ADD CONSTRAINT "gromplate to user. author_id. fkey" FOREIGN KEY (author_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.grompleaf
    ADD CONSTRAINT "grompleaf to gromplate. gromplate_id. fkey" FOREIGN KEY (gromplate_id) REFERENCES public.gromplate(id);


ALTER TABLE ONLY public.history
    ADD CONSTRAINT "history to card. card_id. fkey" FOREIGN KEY (card_id) REFERENCES public.card(id) ON DELETE SET NULL;


ALTER TABLE ONLY public.history
    ADD CONSTRAINT "history to leaf. leaf_id. fkey" FOREIGN KEY (leaf_id) REFERENCES public.leaf(id);


ALTER TABLE ONLY public.history
    ADD CONSTRAINT "history to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.leaf
    ADD CONSTRAINT "leaf to branch. branch_id. fkey" FOREIGN KEY (branch_id) REFERENCES public.branch(id);


ALTER TABLE ONLY public.leaf
    ADD CONSTRAINT "leaf to branch. stack_id, branch_id. fkey" FOREIGN KEY (stack_id, branch_id) REFERENCES public.branch(stack_id, id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.leaf
    ADD CONSTRAINT "leaf to grompleaf. grompleaf_id. fkey" FOREIGN KEY (grompleaf_id) REFERENCES public.grompleaf(id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to branch. branch_id, stack_id. fkey" FOREIGN KEY (branch_id, stack_id) REFERENCES public.branch(id, stack_id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to branch. branch_id. fkey" FOREIGN KEY (branch_id) REFERENCES public.branch(id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to deck. deck_id. fkey" FOREIGN KEY (deck_id) REFERENCES public.deck(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to gromplate. gromplate_id. fkey" FOREIGN KEY (gromplate_id) REFERENCES public.gromplate(id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to grompleaf. grompleaf_id. fkey" FOREIGN KEY (grompleaf_id) REFERENCES public.grompleaf(id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to leaf. leaf_id, branch_id. fkey" FOREIGN KEY (leaf_id, branch_id) REFERENCES public.leaf(id, branch_id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to leaf. leaf_id, stack_id. fkey" FOREIGN KEY (leaf_id, stack_id) REFERENCES public.leaf(id, stack_id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to leaf. leaf_id. fkey" FOREIGN KEY (leaf_id) REFERENCES public.leaf(id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to stack. stack_id. fkey" FOREIGN KEY (stack_id) REFERENCES public.stack(id);


ALTER TABLE ONLY public.notification
    ADD CONSTRAINT "notification to user. sender_id. fkey" FOREIGN KEY (sender_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.received_notification
    ADD CONSTRAINT "received_notification to notification. notification_id. fkey" FOREIGN KEY (notification_id) REFERENCES public.notification(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.received_notification
    ADD CONSTRAINT "received_notification to user. receiver_id. fkey" FOREIGN KEY (receiver_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.relationship_2_card
    ADD CONSTRAINT "relationship_2_card to card. source_card_id, user_id, source_st" FOREIGN KEY (source_card_id, user_id, source_stack_id) REFERENCES public.card(id, user_id, stack_id) ON DELETE CASCADE;


ALTER TABLE ONLY public.relationship_2_card
    ADD CONSTRAINT "relationship_2_card to card. source_card_id. fkey" FOREIGN KEY (source_card_id) REFERENCES public.card(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.relationship_2_card
    ADD CONSTRAINT "relationship_2_card to card. target_card_id, user_id, target_st" FOREIGN KEY (target_card_id, user_id, target_stack_id) REFERENCES public.card(id, user_id, stack_id) ON DELETE CASCADE;


ALTER TABLE ONLY public.relationship_2_card
    ADD CONSTRAINT "relationship_2_card to card. target_card_id. fkey" FOREIGN KEY (target_card_id) REFERENCES public.card(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.relationship_2_card
    ADD CONSTRAINT "relationship_2_card to relationship. relationship_id. fkey" FOREIGN KEY (relationship_id) REFERENCES public.relationship(id);


ALTER TABLE ONLY public.stack
    ADD CONSTRAINT "stack to branch. default_branch_id, id. fkey" FOREIGN KEY (default_branch_id, id) REFERENCES public.branch(id, stack_id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.stack
    ADD CONSTRAINT "stack to leaf. copy_source_id. fkey" FOREIGN KEY (copy_source_id) REFERENCES public.leaf(id);


ALTER TABLE ONLY public.stack
    ADD CONSTRAINT "stack to user. author_id. fkey" FOREIGN KEY (author_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.tag_2_card
    ADD CONSTRAINT "tag_2_card to card. card_id, user_id, stack_id. fkey" FOREIGN KEY (card_id, user_id, stack_id) REFERENCES public.card(id, user_id, stack_id) ON DELETE CASCADE;


ALTER TABLE ONLY public.tag_2_card
    ADD CONSTRAINT "tag_2_card to card. card_id. fkey" FOREIGN KEY (card_id) REFERENCES public.card(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.tag_2_card
    ADD CONSTRAINT "tag_2_card to tag. tag_id. fkey" FOREIGN KEY (tag_id) REFERENCES public.tag(id);


ALTER TABLE ONLY public.tag_2_user_2_grompleaf
    ADD CONSTRAINT "tag_2_user_2_grompleaf to tag. default_tag_id. fkey" FOREIGN KEY (default_tag_id) REFERENCES public.tag(id);


ALTER TABLE ONLY public.tag_2_user_2_grompleaf
    ADD CONSTRAINT "tag_2_user_2_grompleaf to user_2_grompleaf. user_id, grompleaf_" FOREIGN KEY (user_id, grompleaf_id) REFERENCES public.user_2_grompleaf(user_id, grompleaf_id);


ALTER TABLE ONLY public.padawan
    ADD CONSTRAINT "user to card_setting. default_card_setting_id, id. fkey" FOREIGN KEY (default_card_setting_id, id) REFERENCES public.card_setting(id, user_id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.padawan
    ADD CONSTRAINT "user to deck. default_deck_id, id. fkey" FOREIGN KEY (default_deck_id, id) REFERENCES public.deck(id, user_id) DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public.user_2_grompleaf
    ADD CONSTRAINT "user_2_grompleaf to card_setting. default_card_setting_id. fkey" FOREIGN KEY (default_card_setting_id) REFERENCES public.card_setting(id);


ALTER TABLE ONLY public.user_2_grompleaf
    ADD CONSTRAINT "user_2_grompleaf to grompleaf. grompleaf_id. fkey" FOREIGN KEY (grompleaf_id) REFERENCES public.grompleaf(id);


ALTER TABLE ONLY public.user_2_grompleaf
    ADD CONSTRAINT "user_2_grompleaf to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.vote_2_comment_gromplate
    ADD CONSTRAINT "vote_2_comment_gromplate to comment_gromplate. comment_gromplat" FOREIGN KEY (comment_gromplate_id) REFERENCES public.comment_gromplate(id);


ALTER TABLE ONLY public.vote_2_comment_gromplate
    ADD CONSTRAINT "vote_2_comment_gromplate to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.vote_2_comment_stack
    ADD CONSTRAINT "vote_2_comment_stack to comment_stack. comment_stack_id. fkey" FOREIGN KEY (comment_stack_id) REFERENCES public.comment_stack(id);


ALTER TABLE ONLY public.vote_2_comment_stack
    ADD CONSTRAINT "vote_2_comment_stack to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);


ALTER TABLE ONLY public.vote_2_feedback
    ADD CONSTRAINT "vote_2_feedback to feedback. feedback_id. fkey" FOREIGN KEY (feedback_id) REFERENCES public.feedback(id);


ALTER TABLE ONLY public.vote_2_feedback
    ADD CONSTRAINT "vote_2_feedback to user. user_id. fkey" FOREIGN KEY (user_id) REFERENCES public.padawan(id);



