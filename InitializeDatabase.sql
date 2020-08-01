-- medTODO counts involving `"cardState" <> 3` are going to be slightly wrong. They're using CollectedCard, and a Card can have multiple CollectedCards.

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

CREATE TYPE public."notificationType" AS ENUM (
    'DeckAddedStack',
    'DeckUpdatedStack',
    'DeckDeletedStack'
);


ALTER TYPE public."notificationType" OWNER TO postgres;

CREATE FUNCTION public.fn_ctr_branch_insertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        default_branch_id integer NOT NULL := 0;
    BEGIN
        default_branch_id := (SELECT "defaultBranchId" FROM "stack" s WHERE NEW."stackId" = s."id");
        IF ((NEW."name" IS NOT NULL) AND (default_branch_id = NEW."id")) THEN
            RAISE EXCEPTION 'Default Branches must have a null Name. StackId#% with BranchId#% by UserId#% just attempted to be titled "%"', (NEW."stackId"), (NEW."id"), (NEW."authorId"), (NEW."name");
        ELSIF ((NEW."name" IS NULL) AND (default_branch_id <> NEW."id")) THEN
            RAISE EXCEPTION 'Only Default Branches may have a null Name. StackId#% with BranchId#% by UserId#% just attempted to be titled "%"', (NEW."stackId"), (NEW."id"), (NEW."authorId"), (NEW."name");
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_ctr_branch_insertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_ctr_collectedcard_insertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF (1 < (SELECT COUNT(*) FROM (SELECT DISTINCT cc."branchInstanceId" FROM "collectedCard" cc WHERE cc."userId" = NEW."userId" AND cc."stackId" = NEW."stackId") _)) THEN
            RAISE EXCEPTION 'UserId #% with CollectedCard #% and Stack #% tried to have BranchInstanceId #%, but they already have BranchInstanceId #%',
            (NEW."userId"), (NEW."id"), (NEW."stackId"), (NEW."branchInstanceId"), (SELECT cc."branchInstanceId" FROM "collectedCard" cc WHERE cc."userId" = NEW."userId" AND cc."stackId" = NEW."stackId" LIMIT 1);
        END IF;
		IF (TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND (OLD."branchInstanceId" <> NEW."branchInstanceId" OR OLD."index" <> NEW."index"))) THEN
		IF ((SELECT bi."maxIndexInclusive" FROM public."branchInstance" bi WHERE bi."id" = NEW."branchInstanceId") < NEW."index") THEN
			RAISE EXCEPTION 'UserId #% with CollectedCard #% tried to have index %, which exceeds the MaxIndexInclusive value of % on its BranchInstanceId #%', (NEW."userId"), (NEW."id"), (NEW."index"), (SELECT bi."maxIndexInclusive" FROM public."branchInstance" bi WHERE bi."id" = NEW."branchInstanceId"), (NEW."branchInstanceId");
		END IF;
		END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_ctr_collectedcard_insertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_delete_received_notification(notification_id integer, receiver_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
    BEGIN
        WITH del_child AS (
            DELETE FROM public."receivedNotification" rn
            WHERE  rn."notificationId" = notification_id
            AND    rn."receiverId" = receiver_id
            RETURNING rn."notificationId", rn."receiverId"
        )
        DELETE FROM public."notification" n
        USING  del_child x
        WHERE  n."id" = x."notificationId"
        AND NOT EXISTS (
            SELECT 1
            FROM   public."receivedNotification" rn
            WHERE  rn."notificationId" = x."notificationId"
            AND    rn."receiverId" <> x."receiverId"
        );
    END
$$;


ALTER FUNCTION public.fn_delete_received_notification(notification_id integer, receiver_id integer) OWNER TO postgres;

COMMENT ON FUNCTION public.fn_delete_received_notification(notification_id integer, receiver_id integer) IS 'https://stackoverflow.com/a/15810159';


CREATE FUNCTION public.fn_tr_branch_afterinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        default_branch_id integer NOT NULL := 0;
    BEGIN
        IF (TG_OP = 'INSERT') THEN
            UPDATE "stack" s
            SET    "defaultBranchId" = (NEW."id")
            WHERE (s."id" = NEW."stackId" AND s."defaultBranchId" = 0);
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_branch_afterinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_branchinstance_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE "branch" b
  SET    "latestInstanceId" = NEW."id"
  WHERE  b."id" = NEW."branchId";
  IF (NEW."tsVectorHelper" IS NOT NULL) THEN
    NEW."tsVector" = to_tsvector('pg_catalog.english', NEW."tsVectorHelper");
    NEW."tsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_tr_branchinstance_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_collateinstance_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE "collate" t
  SET    "latestInstanceId" = NEW."id"
  WHERE  t."id" = NEW."collateId";
  IF (NEW."cWeightTsVectorHelper" IS NOT NULL) THEN
    NEW."tsVector" =
        setweight(to_tsvector('pg_catalog.english', NEW."name"), 'A') ||
        setweight(to_tsvector('pg_catalog.english', NEW."cWeightTsVectorHelper"), 'C') ||
        setweight(to_tsvector('pg_catalog.english', NEW."css"), 'D');
    NEW."cWeightTsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_tr_collateinstance_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_collectedcard_afterinsertdeleteupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        new_is_public boolean NOT NULL := 'f';
        old_is_public boolean NOT NULL := 'f';
    BEGIN
		IF ((TG_OP = 'DELETE' AND OLD."index" = 0 AND OLD."cardState" <> 3) OR 
            (TG_OP = 'UPDATE' AND (OLD."branchInstanceId" <> NEW."branchInstanceId"
                                      OR (OLD."cardState" <> 3 AND NEW."cardState" = 3)))) THEN
            UPDATE	"branchInstance" ci
            SET     "users" = ci."users" - 1
            WHERE	ci."id" = OLD."branchInstanceId";
            UPDATE	"branch" b
            SET		"users" = b."users" - 1
            WHERE	b."id" = OLD."branchId";
            UPDATE  "stack" stack
            SET     "users" = stack."users" - 1
            WHERE stack."id" = OLD."stackId";
        END IF;
        IF ((TG_OP = 'INSERT' AND NEW."index" = 0) OR
            (TG_OP = 'UPDATE' AND (OLD."branchInstanceId" <> NEW."branchInstanceId"
                                      OR (OLD."cardState" = 3 AND NEW."cardState" <> 3)))) THEN
            UPDATE	"branchInstance" ci
            SET     "users" = ci."users" + 1
            WHERE	ci."id" = NEW."branchInstanceId";
            UPDATE	"branch" b
            SET		"users" = b."users" + 1
            WHERE	b."id" = NEW."branchId";
            UPDATE  "stack" stack
            SET     "users" = stack."users" + 1
            WHERE stack."id" = NEW."stackId";
        END IF;
        new_is_public := COALESCE((SELECT "isPublic" FROM public."deck" WHERE "id" = NEW."deckId"), 'f');
        old_is_public := COALESCE((SELECT "isPublic" FROM public."deck" WHERE "id" = OLD."deckId"), 'f');
        IF (new_is_public OR old_is_public) THEN
            IF (TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND OLD."deckId" <> NEW."deckId" AND new_is_public)) THEN
                WITH notification_id AS (
                    INSERT INTO public."notification"("SenderId", "timeStamp",              "type",          "message",     "stackId",     "branchId",     "branchInstanceId",     "deckId", "collateId", "collateInstanceId")
                                            VALUES (NEW."userId", (timezone('utc', now())), 'DeckAddedStack', NULL,     NEW."stackId", NEW."branchId", NEW."branchInstanceId", NEW."deckId",  NULL,       NULL)
                    RETURNING "id"
                ) INSERT INTO public."receivedNotification"("ReceiverId", "notificationId")
                                                 (SELECT df."followerId", (SELECT "id" FROM notification_id)
                                                  FROM public."deckFollowers" df
                                                  WHERE df."deckId" = NEW."deckId"
                                                 );
            END IF;
            IF (TG_OP = 'UPDATE' AND OLD."branchInstanceId" <> NEW."branchInstanceId") THEN
                WITH notification_id AS (
                    INSERT INTO public."notification"("SenderId", "timeStamp",              "type",          "message",     "stackId",     "branchId",     "branchInstanceId",     "deckId", "collateId", "collateInstanceId")
                                            VALUES (NEW."userId", (timezone('utc', now())), 'DeckUpdatedStack', NULL,   NEW."stackId", NEW."branchId", NEW."branchInstanceId", NEW."deckId",  NULL,       NULL)
                    RETURNING "id"
                ) INSERT INTO public."receivedNotification"("ReceiverId", "notificationId")
                                                 (SELECT df."followerId", (SELECT "id" FROM notification_id)
                                                  FROM public."deckFollowers" df
                                                  WHERE df."deckId" = NEW."deckId"
                                                 );
            END IF;
            IF (TG_OP = 'DELETE' OR (TG_OP = 'UPDATE' AND OLD."deckId" <> NEW."deckId" AND old_is_public)) THEN
                WITH notification_id AS (
                    INSERT INTO public."notification"("SenderId", "timeStamp",              "type",          "message",     "stackId",     "branchId",     "branchInstanceId",     "deckId", "collateId", "collateInstanceId")
                                            VALUES (OLD."userId", (timezone('utc', now())), 'DeckDeletedStack', NULL,   OLD."stackId", OLD."branchId", OLD."branchInstanceId", OLD."deckId",  NULL,       NULL)
                    RETURNING "id"
                ) INSERT INTO public."receivedNotification"("ReceiverId", "notificationId")
                                                 (SELECT df."followerId", (SELECT "id" FROM notification_id)
                                                  FROM public."deckFollowers" df
                                                  WHERE df."deckId" = OLD."deckId"
                                                 );
            END IF;
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_collectedcard_afterinsertdeleteupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_collectedcard_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF (NEW."tsVectorHelper" IS NOT NULL) THEN
            NEW."tsVector" = to_tsvector('pg_catalog.english', NEW."tsVectorHelper");
            NEW."tsVectorHelper" = NULL;
        END IF;
        RETURN NEW;
    END;
$$;


ALTER FUNCTION public.fn_tr_collectedcard_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_communalfieldinstance_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE "communalField" cf
  SET    "latestInstanceId" = NEW."id"
  WHERE  cf."id" = NEW."communalFieldId";
  IF (NEW."bWeightTsVectorHelper" IS NOT NULL) THEN
    NEW."tsVector" =
        setweight(to_tsvector('pg_catalog.english', NEW."fieldName"), 'A') ||
        setweight(to_tsvector('pg_catalog.english', NEW."bWeightTsVectorHelper"), 'B');
    NEW."bWeightTsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_tr_communalfieldinstance_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_deck_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW."tsVector" = to_tsvector('pg_catalog.english', NEW."name");
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_deck_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_deckfollower_afterinsertdeleteupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF TG_OP = 'INSERT' THEN
            UPDATE	"deck" d
            SET     "followers" = d."followers" + 1
            WHERE	d."id" = NEW."deckId";
        ELSIF TG_OP = 'DELETE' THEN
            UPDATE	"deck" d
            SET     "followers" = d."followers" - 1
            WHERE	d."id" = OLD."deckId";
        ELSIF TG_OP = 'UPDATE' THEN
            RAISE EXCEPTION 'Handle the case when "deckFollower" is updated';
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_deckfollower_afterinsertdeleteupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_relationship_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
begin
  NEW."tsVector" = to_tsvector('pg_catalog.english', NEW."name");
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_relationship_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_tag_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW."tsVector" = to_tsvector('pg_catalog.english', NEW."name");
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_tag_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_user_afterinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        default_card_setting_id integer NOT NULL := 0;
        default_deck_id         integer NOT NULL := 0;
    BEGIN
        default_card_setting_id := (SELECT "id" FROM "cardSetting" cs WHERE cs."userId" = 0 LIMIT 1);
        default_deck_id         := (SELECT "id" FROM "deck"         d WHERE  d."userId" = 0 LIMIT 1);

        UPDATE "cardSetting" cs
        SET    "userId" = NEW."id"
        WHERE (cs."id" = default_card_setting_id);
        UPDATE "user" u
        SET    "defaultCardSettingId" = default_card_setting_id
        WHERE (u."id" = NEW."id");

        UPDATE "deck" d
        SET    "userId" = NEW."id"
        WHERE (d."id" = default_deck_id);
        UPDATE "user" u
        SET    "defaultDeckId" = default_deck_id
        WHERE (u."id" = NEW."id");

        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_tr_user_afterinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_tr_user_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW."tsVector" = to_tsvector('pg_catalog.simple', NEW."displayName");
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tr_user_beforeinsertupdate() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE TABLE public."alphaBetaKey" (
    "id" integer NOT NULL,
    "key" character varying(50) NOT NULL,
    "isUsed" boolean NOT NULL
);


ALTER TABLE public."alphaBetaKey" OWNER TO postgres;

ALTER TABLE public."alphaBetaKey" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."alphaBetaKey$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."branch" (
    "id" integer NOT NULL,
    "name" character varying(64),
    "authorId" integer NOT NULL,
    "stackId" integer NOT NULL,
    "latestInstanceId" integer NOT NULL,
    "users" integer NOT NULL,
    "isListed" boolean NOT NULL
);


ALTER TABLE public."branch" OWNER TO postgres;

CREATE TABLE public."branchInstance" (
    "id" integer NOT NULL,
    "created" timestamp without time zone NOT NULL,
    "modified" timestamp without time zone,
    "stackId" integer NOT NULL,
    "branchId" integer NOT NULL,
    "isDmca" boolean NOT NULL,
    "fieldValues" text NOT NULL,
    "collateInstanceId" integer NOT NULL,
    "users" integer NOT NULL,
    "editSummary" character varying(200) NOT NULL,
    "ankiNoteId" bigint,
    "hash" bit(512) NOT NULL,
    "tsVectorHelper" text,
    "tsVector" tsvector,
    "maxIndexInclusive" smallint NOT NULL,
    CONSTRAINT "branchInstance$tsVectorHelper$isNull" CHECK (("tsVectorHelper" IS NULL))
);


ALTER TABLE public."branchInstance" OWNER TO postgres;

CREATE TABLE public."collectedCard" (
    "id" integer NOT NULL,
    "userId" integer NOT NULL,
    "stackId" integer NOT NULL,
    "branchId" integer NOT NULL,
    "branchInstanceId" integer NOT NULL,
    "index" smallint NOT NULL,
    "cardState" smallint NOT NULL,
    "easeFactorInPermille" smallint NOT NULL,
    "intervalOrStepsIndex" smallint NOT NULL,
    "due" timestamp without time zone NOT NULL,
    "cardSettingId" integer NOT NULL,
    "deckId" integer NOT NULL,
    "isLapsed" boolean NOT NULL,
    "frontPersonalField" text NOT NULL,
    "backPersonalField" text NOT NULL,
    "tsVectorHelper" text,
    "tsVector" tsvector,
    CONSTRAINT "collectedCard$tsVectorHelper$isNull" CHECK (("tsVectorHelper" IS NULL))
);


ALTER TABLE public."collectedCard" OWNER TO postgres;

CREATE TABLE public."relationship" (
    "id" integer NOT NULL,
    "name" character varying(250) NOT NULL,
    "tsVector" tsvector
);


ALTER TABLE public."relationship" OWNER TO postgres;

CREATE TABLE public."relationship$collectedCard" (
    "relationshipId" integer NOT NULL,
    "userId" integer NOT NULL,
    "sourceStackId" integer NOT NULL,
    "targetStackId" integer NOT NULL,
    "sourceCollectedCardId" integer NOT NULL,
    "targetCollectedCardId" integer NOT NULL
);


ALTER TABLE public."relationship$collectedCard" OWNER TO postgres;

CREATE VIEW public."branchInstanceRelationshipCount" AS
 SELECT sac."branchInstanceId" AS "sourceBranchInstanceId",
    tac."branchInstanceId" AS "targetBranchInstanceId",
    unnest(ARRAY[sac."branchInstanceId", tac."branchInstanceId"]) AS "branchInstanceId",
    ( SELECT r."name"
           FROM public."relationship" r
          WHERE (r."id" = rac."relationshipId")
         LIMIT 1) AS "name",
    count(*) AS "count"
   FROM ((public."relationship$collectedCard" rac
     JOIN public."collectedCard" sac ON ((rac."sourceCollectedCardId" = sac."id")))
     JOIN public."collectedCard" tac ON ((rac."targetCollectedCardId" = tac."id")))
  WHERE ((sac."cardState" <> 3) AND (tac."cardState" <> 3))
  GROUP BY sac."branchInstanceId", tac."branchInstanceId", rac."relationshipId";


ALTER TABLE public."branchInstanceRelationshipCount" OWNER TO postgres;

CREATE TABLE public."tag" (
    "id" integer NOT NULL,
    "name" character varying(250) NOT NULL,
    "tsVector" tsvector
);


ALTER TABLE public."tag" OWNER TO postgres;

CREATE TABLE public."tag$collectedCard" (
    "tagId" integer NOT NULL,
    "userId" integer NOT NULL,
    "stackId" integer NOT NULL,
    "collectedCardId" integer NOT NULL
);


ALTER TABLE public."tag$collectedCard" OWNER TO postgres;

CREATE VIEW public."branchInstanceTagCount" AS
 SELECT i."id" AS "branchInstanceId",
    ( SELECT t."name"
           FROM public."tag" t
          WHERE (t."id" = ta."tagId")
         LIMIT 1) AS "name",
    count(*) AS "count"
   FROM ((public."branchInstance" i
     JOIN public."collectedCard" cc ON ((cc."branchInstanceId" = i."id")))
     JOIN public."tag$collectedCard" ta ON ((ta."collectedCardId" = cc."id")))
  WHERE (cc."cardState" <> 3)
  GROUP BY i."id", ta."tagId";


ALTER TABLE public."branchInstanceTagCount" OWNER TO postgres;

ALTER TABLE public."branchInstance" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."branchInstance$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."branch" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."branch$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."cardSetting" (
    "id" integer NOT NULL,
    "userId" integer NOT NULL,
    "name" character varying(100) NOT NULL,
    "newCardsStepsInMinutes" character varying(100) NOT NULL,
    "newCardsMaxPerDay" smallint NOT NULL,
    "newCardsGraduatingIntervalInDays" smallint NOT NULL,
    "newCardsEasyIntervalInDays" smallint NOT NULL,
    "newCardsStartingEaseFactorInPermille" smallint NOT NULL,
    "newCardsBuryRelated" boolean NOT NULL,
    "matureCardsMaxPerDay" smallint NOT NULL,
    "matureCardsEaseFactorEasyBonusFactorInPermille" smallint NOT NULL,
    "matureCardsIntervalFactorInPermille" smallint NOT NULL,
    "matureCardsMaximumIntervalInDays" smallint NOT NULL,
    "matureCardsHardIntervalFactorInPermille" smallint NOT NULL,
    "matureCardsBuryRelated" boolean NOT NULL,
    "lapsedCardsStepsInMinutes" character varying(100) NOT NULL,
    "lapsedCardsNewIntervalFactorInPermille" smallint NOT NULL,
    "lapsedCardsMinimumIntervalInDays" smallint NOT NULL,
    "lapsedCardsLeechThreshold" smallint NOT NULL,
    "showAnswerTimer" boolean NOT NULL,
    "automaticallyPlayAudio" boolean NOT NULL,
    "replayQuestionAudioOnAnswer" boolean NOT NULL
);


ALTER TABLE public."cardSetting" OWNER TO postgres;

ALTER TABLE public."cardSetting" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."cardSetting$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."collate" (
    "id" integer NOT NULL,
    "authorId" integer NOT NULL,
    "latestInstanceId" integer NOT NULL,
    "isListed" boolean NOT NULL
);


ALTER TABLE public."collate" OWNER TO postgres;

CREATE TABLE public."collateInstance" (
    "id" integer NOT NULL,
    "name" character varying(100) NOT NULL,
    "collateId" integer NOT NULL,
    "css" character varying(4000) NOT NULL,
    "created" timestamp without time zone NOT NULL,
    "modified" timestamp without time zone,
    "latexPre" character varying(500) NOT NULL,
    "latexPost" character varying(500) NOT NULL,
    "isDmca" boolean NOT NULL,
    "templates" text NOT NULL,
    "type" smallint NOT NULL,
    "fields" character varying(4000) NOT NULL,
    "editSummary" character varying(200) NOT NULL,
    "ankiId" bigint,
    "hash" bit(512) NOT NULL,
    "cWeightTsVectorHelper" text,
    "tsVector" tsvector,
    CONSTRAINT "collateInstance$cWeightTsVectorHelper$isNull" CHECK (("cWeightTsVectorHelper" IS NULL))
);


ALTER TABLE public."collateInstance" OWNER TO postgres;

ALTER TABLE public."collateInstance" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."collateInstance$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."collate" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."collate$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE VIEW public."collectedCardIsLatest" AS
 SELECT a."id",
    a."userId",
    a."stackId",
    a."branchId",
    a."branchInstanceId",
    a."index",
    a."cardState",
    a."easeFactorInPermille",
    a."intervalOrStepsIndex",
    a."due",
    a."cardSettingId",
    a."isLapsed",
    a."frontPersonalField",
    a."backPersonalField",
    a."deckId",
    (b."latestInstanceId" IS NULL) AS "isLatest"
   FROM (public."collectedCard" a
     LEFT JOIN public."branch" b ON ((b."latestInstanceId" = a."branchInstanceId")));


ALTER TABLE public."collectedCardIsLatest" OWNER TO postgres;

ALTER TABLE public."collectedCard" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."collectedCard$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."commentCollate" (
    "id" integer NOT NULL,
    "collateId" integer NOT NULL,
    "userId" integer NOT NULL,
    "text" character varying(500) NOT NULL,
    "created" timestamp without time zone NOT NULL,
    "isDmca" boolean NOT NULL
);


ALTER TABLE public."commentCollate" OWNER TO postgres;

ALTER TABLE public."commentCollate" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."commentCollate$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."commentStack" (
    "id" integer NOT NULL,
    "stackId" integer NOT NULL,
    "userId" integer NOT NULL,
    "text" character varying(500) NOT NULL,
    "created" timestamp without time zone NOT NULL,
    "isDmca" boolean NOT NULL
);


ALTER TABLE public."commentStack" OWNER TO postgres;

ALTER TABLE public."commentStack" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."commentStack$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."communalField" (
    "id" integer NOT NULL,
    "authorId" integer NOT NULL,
    "latestInstanceId" integer NOT NULL,
    "isListed" boolean NOT NULL
);


ALTER TABLE public."communalField" OWNER TO postgres;

CREATE TABLE public."communalFieldInstance" (
    "id" integer NOT NULL,
    "communalFieldId" integer NOT NULL,
    "fieldName" character varying(200) NOT NULL,
    "value" text NOT NULL,
    "created" timestamp without time zone NOT NULL,
    "modified" timestamp without time zone,
    "editSummary" character varying(200) NOT NULL,
    "bWeightTsVectorHelper" text,
    "tsVector" tsvector,
    CONSTRAINT "communalFieldInstance$bWeightTsVectorHelper$isNull" CHECK (("bWeightTsVectorHelper" IS NULL))
);


ALTER TABLE public."communalFieldInstance" OWNER TO postgres;

CREATE TABLE public."communalFieldInstance$branchInstance" (
    "branchInstanceId" integer NOT NULL,
    "communalFieldInstanceId" integer NOT NULL
);


ALTER TABLE public."communalFieldInstance$branchInstance" OWNER TO postgres;

ALTER TABLE public."communalFieldInstance" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."communalFieldInstance$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."communalField" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."communalField$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."deck" (
    "id" integer NOT NULL,
    "userId" integer NOT NULL,
    "name" character varying(250) NOT NULL,
    "isPublic" boolean NOT NULL,
    "sourceId" integer,
    "followers" integer NOT NULL,
    "tsVector" tsvector
);


ALTER TABLE public."deck" OWNER TO postgres;

CREATE TABLE public."deckFollowers" (
    "deckId" integer NOT NULL,
    "followerId" integer NOT NULL
);


ALTER TABLE public."deckFollowers" OWNER TO postgres;

ALTER TABLE public."deck" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."deck$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."feedback" (
    "id" integer NOT NULL,
    "title" character varying(50) NOT NULL,
    "description" character varying(1000) NOT NULL,
    "userId" integer NOT NULL,
    "created" timestamp without time zone NOT NULL,
    "parentId" integer,
    "priority" smallint
);


ALTER TABLE public."feedback" OWNER TO postgres;

ALTER TABLE public."feedback" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."feedback$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."file" (
    "id" integer NOT NULL,
    "fileName" character varying(200) NOT NULL,
    "data" bytea NOT NULL,
    "sha256" bytea NOT NULL
);


ALTER TABLE public."file" OWNER TO postgres;

CREATE TABLE public."file$branchInstance" (
    "branchInstanceId" integer NOT NULL,
    "fileId" integer NOT NULL
);


ALTER TABLE public."file$branchInstance" OWNER TO postgres;

ALTER TABLE public."file" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."file$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."filter" (
    "id" integer NOT NULL,
    "name" character varying(128) NOT NULL,
    "userId" integer NOT NULL,
    "query" character varying(256) NOT NULL
);


ALTER TABLE public."filter" OWNER TO postgres;

ALTER TABLE public."filter" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."filter$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."history" (
    "id" bigint NOT NULL,
    "collectedCardId" integer,
    "userId" integer NOT NULL,
    "branchInstanceId" integer,
    "index" smallint NOT NULL,
    "score" smallint NOT NULL,
    "timestamp" timestamp without time zone NOT NULL,
    "intervalWithUnusedStepsIndex" smallint NOT NULL,
    "easeFactorInPermille" smallint NOT NULL,
    "timeFromSeeingQuestionToScoreInSecondsPlus32768" smallint NOT NULL
);


ALTER TABLE public."history" OWNER TO postgres;

ALTER TABLE public."history" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."history$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."notification" (
    "id" integer NOT NULL,
    "senderId" integer NOT NULL,
    "timeStamp" timestamp without time zone NOT NULL,
    "type" public."notificationType" NOT NULL,
    "message" character varying(4000),
    "stackId" integer,
    "branchId" integer,
    "branchInstanceId" integer,
    "deckId" integer,
    "collateId" integer,
    "collateInstanceId" integer
);


ALTER TABLE public."notification" OWNER TO postgres;

ALTER TABLE public."notification" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."notification$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."potentialSignups" (
    "id" integer NOT NULL,
    "email" character varying(500) NOT NULL,
    "message" character varying(1000) NOT NULL,
    "oneIsAlpha2Beta3Ga" smallint NOT NULL,
    "timeStamp" timestamp without time zone NOT NULL
);


ALTER TABLE public."potentialSignups" OWNER TO postgres;

ALTER TABLE public."potentialSignups" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."potentialSignups$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."receivedNotification" (
    "receiverId" integer NOT NULL,
    "notificationId" integer NOT NULL
);


ALTER TABLE public."receivedNotification" OWNER TO postgres;

ALTER TABLE public."relationship" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."relationship$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."stack" (
    "id" integer NOT NULL,
    "authorId" integer NOT NULL,
    "users" integer NOT NULL,
    "copySourceId" integer,
    "defaultBranchId" integer NOT NULL,
    "isListed" boolean NOT NULL
);


ALTER TABLE public."stack" OWNER TO postgres;

CREATE VIEW public."stackRelationshipCount" AS
 SELECT sac."stackId" AS "sourceStackId",
    tac."stackId" AS "targetStackId",
    unnest(ARRAY[sac."stackId", tac."stackId"]) AS "stackId",
    ( SELECT r."name"
           FROM public."relationship" r
          WHERE (r."id" = rac."relationshipId")
         LIMIT 1) AS "name",
    count(*) AS "count"
   FROM ((public."relationship$collectedCard" rac
     JOIN public."collectedCard" sac ON ((rac."sourceCollectedCardId" = sac."id")))
     JOIN public."collectedCard" tac ON ((rac."targetCollectedCardId" = tac."id")))
  WHERE ((sac."cardState" <> 3) AND (tac."cardState" <> 3))
  GROUP BY sac."stackId", tac."stackId", rac."relationshipId";


ALTER TABLE public."stackRelationshipCount" OWNER TO postgres;

CREATE VIEW public."stackTagCount" AS
 SELECT s."id" AS "stackId",
    ( SELECT t."name"
           FROM public."tag" t
          WHERE (t."id" = ta."tagId")
         LIMIT 1) AS "name",
    count(*) AS "count"
   FROM ((public."stack" s
     JOIN public."collectedCard" cc ON ((cc."stackId" = s."id")))
     JOIN public."tag$collectedCard" ta ON ((ta."collectedCardId" = cc."id")))
  WHERE (cc."cardState" <> 3)
  GROUP BY s."id", ta."tagId";


ALTER TABLE public."stackTagCount" OWNER TO postgres;

ALTER TABLE public."stack" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."stack$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."tag" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."tag$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."tag$user$collateInstance" (
    "userId" integer NOT NULL,
    "collateInstanceId" integer NOT NULL,
    "defaultTagId" integer NOT NULL
);


ALTER TABLE public."tag$user$collateInstance" OWNER TO postgres;

CREATE TABLE public."user" (
    "id" integer NOT NULL,
    "displayName" character varying(32) NOT NULL,
    "defaultCardSettingId" integer NOT NULL,
    "defaultDeckId" integer NOT NULL,
    "showNextReviewTime" boolean NOT NULL,
    "showRemainingCardCount" boolean NOT NULL,
    "mixNewAndReview" smallint NOT NULL,
    "nextDayStartsAtXHoursPastMidnight" smallint NOT NULL,
    "learnAheadLimitInMinutes" smallint NOT NULL,
    "timeboxTimeLimitInMinutes" smallint NOT NULL,
    "isNightMode" boolean NOT NULL,
    "tsVector" tsvector
);


ALTER TABLE public."user" OWNER TO postgres;

CREATE TABLE public."user$collateInstance" (
    "userId" integer NOT NULL,
    "collateInstanceId" integer NOT NULL,
    "defaultCardSettingId" integer NOT NULL
);


ALTER TABLE public."user$collateInstance" OWNER TO postgres;

ALTER TABLE public."user" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."user$id$seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."vote$commentCollate" (
    "commentCollateId" integer NOT NULL,
    "userId" integer NOT NULL
);


ALTER TABLE public."vote$commentCollate" OWNER TO postgres;

CREATE TABLE public."vote$commentStack" (
    "commentStackId" integer NOT NULL,
    "userId" integer NOT NULL
);


ALTER TABLE public."vote$commentStack" OWNER TO postgres;

CREATE TABLE public."vote$feedback" (
    "feedbackId" integer NOT NULL,
    "userId" integer NOT NULL
);


ALTER TABLE public."vote$feedback" OWNER TO postgres;







INSERT INTO public."cardSetting" ("id", "userId", "name", "newCardsStepsInMinutes", "newCardsMaxPerDay", "newCardsGraduatingIntervalInDays", "newCardsEasyIntervalInDays", "newCardsStartingEaseFactorInPermille", "newCardsBuryRelated", "matureCardsMaxPerDay", "matureCardsEaseFactorEasyBonusFactorInPermille", "matureCardsIntervalFactorInPermille", "matureCardsMaximumIntervalInDays", "matureCardsHardIntervalFactorInPermille", "matureCardsBuryRelated", "lapsedCardsStepsInMinutes", "lapsedCardsNewIntervalFactorInPermille", "lapsedCardsMinimumIntervalInDays", "lapsedCardsLeechThreshold", "showAnswerTimer", "automaticallyPlayAudio", "replayQuestionAudioOnAnswer") VALUES (1, 1, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public."cardSetting" ("id", "userId", "name", "newCardsStepsInMinutes", "newCardsMaxPerDay", "newCardsGraduatingIntervalInDays", "newCardsEasyIntervalInDays", "newCardsStartingEaseFactorInPermille", "newCardsBuryRelated", "matureCardsMaxPerDay", "matureCardsEaseFactorEasyBonusFactorInPermille", "matureCardsIntervalFactorInPermille", "matureCardsMaximumIntervalInDays", "matureCardsHardIntervalFactorInPermille", "matureCardsBuryRelated", "lapsedCardsStepsInMinutes", "lapsedCardsNewIntervalFactorInPermille", "lapsedCardsMinimumIntervalInDays", "lapsedCardsLeechThreshold", "showAnswerTimer", "automaticallyPlayAudio", "replayQuestionAudioOnAnswer") VALUES (2, 2, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public."cardSetting" ("id", "userId", "name", "newCardsStepsInMinutes", "newCardsMaxPerDay", "newCardsGraduatingIntervalInDays", "newCardsEasyIntervalInDays", "newCardsStartingEaseFactorInPermille", "newCardsBuryRelated", "matureCardsMaxPerDay", "matureCardsEaseFactorEasyBonusFactorInPermille", "matureCardsIntervalFactorInPermille", "matureCardsMaximumIntervalInDays", "matureCardsHardIntervalFactorInPermille", "matureCardsBuryRelated", "lapsedCardsStepsInMinutes", "lapsedCardsNewIntervalFactorInPermille", "lapsedCardsMinimumIntervalInDays", "lapsedCardsLeechThreshold", "showAnswerTimer", "automaticallyPlayAudio", "replayQuestionAudioOnAnswer") VALUES (3, 3, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);


INSERT INTO public."collate" ("id", "authorId", "latestInstanceId", "isListed") VALUES (1, 2, 1001, true);
INSERT INTO public."collate" ("id", "authorId", "latestInstanceId", "isListed") VALUES (2, 2, 1002, true);
INSERT INTO public."collate" ("id", "authorId", "latestInstanceId", "isListed") VALUES (3, 2, 1003, true);
INSERT INTO public."collate" ("id", "authorId", "latestInstanceId", "isListed") VALUES (4, 2, 1006, true);
INSERT INTO public."collate" ("id", "authorId", "latestInstanceId", "isListed") VALUES (5, 2, 1007, true);


INSERT INTO public."collateInstance" ("id", "name", "collateId", "css", "created", "modified", "latexPre", "latexPost", "isDmca", "templates", "type", "fields", "editSummary", "ankiId", "hash", "cWeightTsVectorHelper", "tsVector") VALUES (1001, 'Basic', 1, '.card {
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
INSERT INTO public."collateInstance" ("id", "name", "collateId", "css", "created", "modified", "latexPre", "latexPost", "isDmca", "templates", "type", "fields", "editSummary", "ankiId", "hash", "cWeightTsVectorHelper", "tsVector") VALUES (1002, 'Basic (optional reversed card)', 2, '.card {
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
INSERT INTO public."collateInstance" ("id", "name", "collateId", "css", "created", "modified", "latexPre", "latexPost", "isDmca", "templates", "type", "fields", "editSummary", "ankiId", "hash", "cWeightTsVectorHelper", "tsVector") VALUES (1003, 'Basic (and reversed card)', 3, '.card {
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
INSERT INTO public."collateInstance" ("id", "name", "collateId", "css", "created", "modified", "latexPre", "latexPost", "isDmca", "templates", "type", "fields", "editSummary", "ankiId", "hash", "cWeightTsVectorHelper", "tsVector") VALUES (1004, 'Basic (type in the answer)', 4, '.card {
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
INSERT INTO public."collateInstance" ("id", "name", "collateId", "css", "created", "modified", "latexPre", "latexPost", "isDmca", "templates", "type", "fields", "editSummary", "ankiId", "hash", "cWeightTsVectorHelper", "tsVector") VALUES (1005, 'Cloze', 5, '.card {
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
INSERT INTO public."collateInstance" ("id", "name", "collateId", "css", "created", "modified", "latexPre", "latexPost", "isDmca", "templates", "type", "fields", "editSummary", "ankiId", "hash", "cWeightTsVectorHelper", "tsVector") VALUES (1006, 'Basic (type in the answer)', 4, '.card {
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
INSERT INTO public."collateInstance" ("id", "name", "collateId", "css", "created", "modified", "latexPre", "latexPost", "isDmca", "templates", "type", "fields", "editSummary", "ankiId", "hash", "cWeightTsVectorHelper", "tsVector") VALUES (1007, 'Cloze', 5, '.card {
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














INSERT INTO public."deck" ("id", "userId", "name", "isPublic", "sourceId", "followers", "tsVector") VALUES (1, 1, 'Default Deck', false, NULL, 0, NULL);
INSERT INTO public."deck" ("id", "userId", "name", "isPublic", "sourceId", "followers", "tsVector") VALUES (2, 2, 'Default Deck', false, NULL, 0, NULL);
INSERT INTO public."deck" ("id", "userId", "name", "isPublic", "sourceId", "followers", "tsVector") VALUES (3, 3, 'Default Deck', false, NULL, 0, NULL);
































INSERT INTO public."user" ("id", "displayName", "defaultCardSettingId", "defaultDeckId", "showNextReviewTime", "showRemainingCardCount", "mixNewAndReview", "nextDayStartsAtXHoursPastMidnight", "learnAheadLimitInMinutes", "timeboxTimeLimitInMinutes", "isNightMode", "tsVector") VALUES (1, 'Admin', 1, 1, true, true, 0, 4, 20, 0, false, '''admin'':1');
INSERT INTO public."user" ("id", "displayName", "defaultCardSettingId", "defaultDeckId", "showNextReviewTime", "showRemainingCardCount", "mixNewAndReview", "nextDayStartsAtXHoursPastMidnight", "learnAheadLimitInMinutes", "timeboxTimeLimitInMinutes", "isNightMode", "tsVector") VALUES (2, 'The Collective', 2, 2, true, true, 0, 4, 20, 0, false, '''collective'':2 ''the'':1');
INSERT INTO public."user" ("id", "displayName", "defaultCardSettingId", "defaultDeckId", "showNextReviewTime", "showRemainingCardCount", "mixNewAndReview", "nextDayStartsAtXHoursPastMidnight", "learnAheadLimitInMinutes", "timeboxTimeLimitInMinutes", "isNightMode", "tsVector") VALUES (3, 'RoboTurtle', 3, 3, true, true, 0, 4, 20, 0, false, '''roboturtle'':1');


INSERT INTO public."user$collateInstance" ("userId", "collateInstanceId", "defaultCardSettingId") VALUES (3, 1001, 3);
INSERT INTO public."user$collateInstance" ("userId", "collateInstanceId", "defaultCardSettingId") VALUES (3, 1002, 3);
INSERT INTO public."user$collateInstance" ("userId", "collateInstanceId", "defaultCardSettingId") VALUES (3, 1003, 3);
INSERT INTO public."user$collateInstance" ("userId", "collateInstanceId", "defaultCardSettingId") VALUES (3, 1006, 3);
INSERT INTO public."user$collateInstance" ("userId", "collateInstanceId", "defaultCardSettingId") VALUES (3, 1005, 3);








SELECT pg_catalog.setval('public."alphaBetaKey$id$seq"', 1, false);


SELECT pg_catalog.setval('public."branchInstance$id$seq"', 1001, false);


SELECT pg_catalog.setval('public."branch$id$seq"', 1, false);


SELECT pg_catalog.setval('public."cardSetting$id$seq"', 4, false);


SELECT pg_catalog.setval('public."collateInstance$id$seq"', 1008, false);


SELECT pg_catalog.setval('public."collate$id$seq"', 6, false);


SELECT pg_catalog.setval('public."collectedCard$id$seq"', 1, false);


SELECT pg_catalog.setval('public."commentCollate$id$seq"', 1, false);


SELECT pg_catalog.setval('public."commentStack$id$seq"', 1, false);


SELECT pg_catalog.setval('public."communalFieldInstance$id$seq"', 1001, false);


SELECT pg_catalog.setval('public."communalField$id$seq"', 1, false);


SELECT pg_catalog.setval('public."deck$id$seq"', 4, false);


SELECT pg_catalog.setval('public."feedback$id$seq"', 1, false);


SELECT pg_catalog.setval('public."file$id$seq"', 1, false);


SELECT pg_catalog.setval('public."filter$id$seq"', 1, false);


SELECT pg_catalog.setval('public."history$id$seq"', 1, false);


SELECT pg_catalog.setval('public."notification$id$seq"', 1, false);


SELECT pg_catalog.setval('public."potentialSignups$id$seq"', 1, false);


SELECT pg_catalog.setval('public."relationship$id$seq"', 1, false);


SELECT pg_catalog.setval('public."stack$id$seq"', 1, false);


SELECT pg_catalog.setval('public."tag$id$seq"', 1, false);


SELECT pg_catalog.setval('public."user$id$seq"', 4, false);


ALTER TABLE ONLY public."alphaBetaKey"
    ADD CONSTRAINT "pK$alphaBetaKey" PRIMARY KEY ("id");


ALTER TABLE ONLY public."branch"
    ADD CONSTRAINT "pK$branch" PRIMARY KEY ("id");


ALTER TABLE ONLY public."branchInstance"
    ADD CONSTRAINT "pK$branchInstance" PRIMARY KEY ("id");


ALTER TABLE ONLY public."cardSetting"
    ADD CONSTRAINT "pK$cardSetting" PRIMARY KEY ("id");


ALTER TABLE ONLY public."collate"
    ADD CONSTRAINT "pK$collate" PRIMARY KEY ("id");


ALTER TABLE ONLY public."collateInstance"
    ADD CONSTRAINT "pK$collateInstance" PRIMARY KEY ("id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "pK$collectedCard" PRIMARY KEY ("id");


ALTER TABLE ONLY public."commentCollate"
    ADD CONSTRAINT "pK$commentCollate" PRIMARY KEY ("id");


ALTER TABLE ONLY public."commentStack"
    ADD CONSTRAINT "pK$commentStack" PRIMARY KEY ("id");


ALTER TABLE ONLY public."communalField"
    ADD CONSTRAINT "pK$communalField" PRIMARY KEY ("id");


ALTER TABLE ONLY public."communalFieldInstance"
    ADD CONSTRAINT "pK$communalFieldInstance" PRIMARY KEY ("id");


ALTER TABLE ONLY public."communalFieldInstance$branchInstance"
    ADD CONSTRAINT "pK$communalFieldInstance$branchInstance" PRIMARY KEY ("communalFieldInstanceId", "branchInstanceId");


ALTER TABLE ONLY public."deck"
    ADD CONSTRAINT "pK$deck" PRIMARY KEY ("id");


ALTER TABLE ONLY public."deckFollowers"
    ADD CONSTRAINT "pK$deckFollowers" PRIMARY KEY ("deckId", "followerId");


ALTER TABLE ONLY public."feedback"
    ADD CONSTRAINT "pK$feedback" PRIMARY KEY ("id");


ALTER TABLE ONLY public."file"
    ADD CONSTRAINT "pK$file" PRIMARY KEY ("id");


ALTER TABLE ONLY public."file$branchInstance"
    ADD CONSTRAINT "pK$file$branchInstance" PRIMARY KEY ("branchInstanceId", "fileId");


ALTER TABLE ONLY public."filter"
    ADD CONSTRAINT "pK$filter" PRIMARY KEY ("id");


ALTER TABLE ONLY public."history"
    ADD CONSTRAINT "pK$history" PRIMARY KEY ("id");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "pK$notification" PRIMARY KEY ("id");


ALTER TABLE ONLY public."potentialSignups"
    ADD CONSTRAINT "pK$potentialSignups" PRIMARY KEY ("id");


ALTER TABLE ONLY public."receivedNotification"
    ADD CONSTRAINT "pK$receivedNotification" PRIMARY KEY ("notificationId", "receiverId");


ALTER TABLE ONLY public."relationship"
    ADD CONSTRAINT "pK$relationship" PRIMARY KEY ("id");


ALTER TABLE ONLY public."relationship$collectedCard"
    ADD CONSTRAINT "pK$relationship$collectedCard" PRIMARY KEY ("sourceStackId", "targetStackId", "relationshipId", "userId");


ALTER TABLE ONLY public."stack"
    ADD CONSTRAINT "pK$stack" PRIMARY KEY ("id");


ALTER TABLE ONLY public."tag"
    ADD CONSTRAINT "pK$tag" PRIMARY KEY ("id");


ALTER TABLE ONLY public."tag$collectedCard"
    ADD CONSTRAINT "pK$tag$collectedCard" PRIMARY KEY ("stackId", "tagId", "userId");


ALTER TABLE ONLY public."tag$user$collateInstance"
    ADD CONSTRAINT "pK$tag$user_CollateInstance" PRIMARY KEY ("defaultTagId", "collateInstanceId", "userId");


ALTER TABLE ONLY public."user"
    ADD CONSTRAINT "pK$user" PRIMARY KEY ("id");


ALTER TABLE ONLY public."user$collateInstance"
    ADD CONSTRAINT "pK$user$collateInstance" PRIMARY KEY ("collateInstanceId", "userId");


ALTER TABLE ONLY public."vote$commentCollate"
    ADD CONSTRAINT "pK$vote$commentCollate" PRIMARY KEY ("commentCollateId", "userId");


ALTER TABLE ONLY public."vote$commentStack"
    ADD CONSTRAINT "pK$vote$commentStack" PRIMARY KEY ("commentStackId", "userId");


ALTER TABLE ONLY public."vote$feedback"
    ADD CONSTRAINT "pK$vote$feedback" PRIMARY KEY ("feedbackId", "userId");


ALTER TABLE ONLY public."branchInstance"
    ADD CONSTRAINT "uQ$branchInstance$id_BranchId" UNIQUE ("id", "branchId");


ALTER TABLE ONLY public."branchInstance"
    ADD CONSTRAINT "uQ$branchInstance$id_StackId" UNIQUE ("id", "stackId");


ALTER TABLE ONLY public."branch"
    ADD CONSTRAINT "uQ$branch$branchId_StackId" UNIQUE ("id", "stackId");


ALTER TABLE ONLY public."cardSetting"
    ADD CONSTRAINT "uQ$cardSetting$cardSettingId_UserId" UNIQUE ("id", "userId");


ALTER TABLE ONLY public."collateInstance"
    ADD CONSTRAINT "uQ$collateInstance$collateInstanceId_CollateId" UNIQUE ("id", "collateId");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "uQ$collectedCard$collectedCardId_UserId_StackId" UNIQUE ("id", "stackId", "userId");


ALTER TABLE ONLY public."communalFieldInstance"
    ADD CONSTRAINT "uQ$communalFieldInstance$communalFieldInstanceId_UserId" UNIQUE ("id", "communalFieldId");


ALTER TABLE ONLY public."deck"
    ADD CONSTRAINT "uQ$deck$deckId_UserId" UNIQUE ("id", "userId");


CREATE UNIQUE INDEX "iX$alphaBetaKey$key" ON public."alphaBetaKey" USING btree ("key");


CREATE INDEX "iX$branchInstance$branchId" ON public."branchInstance" USING btree ("branchId");


CREATE INDEX "iX$branchInstance$collateInstanceId" ON public."branchInstance" USING btree ("collateInstanceId");


CREATE INDEX "iX$branchInstance$hash" ON public."branchInstance" USING btree ("hash");


CREATE INDEX "iX$cardSetting$userId" ON public."cardSetting" USING btree ("userId");


CREATE INDEX "iX$collateInstance$collateId" ON public."collateInstance" USING btree ("collateId");


CREATE INDEX "iX$collateInstance$hash" ON public."collateInstance" USING btree ("hash");


CREATE INDEX "iX$collate$authorId" ON public."collate" USING btree ("authorId");


CREATE INDEX "iX$collectedCard$branchInstanceId" ON public."collectedCard" USING btree ("branchInstanceId");


CREATE INDEX "iX$collectedCard$cardSettingId" ON public."collectedCard" USING btree ("cardSettingId");


CREATE INDEX "iX$collectedCard$cardState" ON public."collectedCard" USING btree ("cardState");


CREATE INDEX "iX$collectedCard$userId" ON public."collectedCard" USING btree ("userId");


CREATE INDEX "iX$collectedCard$userId_BranchId" ON public."collectedCard" USING btree ("userId", "branchId");


CREATE UNIQUE INDEX "iX$collectedCard$userId_BranchInstanceId_Index" ON public."collectedCard" USING btree ("userId", "branchInstanceId", "index");


CREATE INDEX "iX$collectedCard$userId_StackId" ON public."collectedCard" USING btree ("userId", "stackId");


CREATE INDEX "iX$commentCollate$collateId" ON public."commentCollate" USING btree ("collateId");


CREATE INDEX "iX$commentCollate$userId" ON public."commentCollate" USING btree ("userId");


CREATE INDEX "iX$commentStack$stackId" ON public."commentStack" USING btree ("stackId");


CREATE INDEX "iX$commentStack$userId" ON public."commentStack" USING btree ("userId");


CREATE INDEX "iX$communalFieldInstance$branchInstance_BranchInstanceId" ON public."communalFieldInstance$branchInstance" USING btree ("branchInstanceId");


CREATE INDEX "iX$communalFieldInstance$communalFieldId" ON public."communalFieldInstance" USING btree ("communalFieldId");


CREATE INDEX "iX$communalField$authorId" ON public."communalField" USING btree ("authorId");


CREATE INDEX "iX$feedback$parentId" ON public."feedback" USING btree ("parentId");


CREATE INDEX "iX$feedback$userId" ON public."feedback" USING btree ("userId");


CREATE INDEX "iX$file$branchInstance_FileId" ON public."file$branchInstance" USING btree ("fileId");


CREATE UNIQUE INDEX "iX$file$sha256" ON public."file" USING btree ("sha256");


CREATE INDEX "iX$filter$userId" ON public."filter" USING btree ("userId");


CREATE INDEX "iX$history$collectedCardId" ON public."history" USING btree ("collectedCardId");


CREATE INDEX "iX$relationship$collectedCard_RelationshipId" ON public."relationship$collectedCard" USING btree ("relationshipId");


CREATE INDEX "iX$relationship$collectedCard_SourceCollectedCardId" ON public."relationship$collectedCard" USING btree ("sourceCollectedCardId");


CREATE INDEX "iX$relationship$collectedCard_TargetCollectedCardId" ON public."relationship$collectedCard" USING btree ("targetCollectedCardId");


CREATE UNIQUE INDEX "iX$relationship$name" ON public."relationship" USING btree (upper(("name")::text));


CREATE INDEX "iX$stack$authorId" ON public."stack" USING btree ("authorId");


CREATE INDEX "iX$tag$collectedCard_CollectedCardId" ON public."tag$collectedCard" USING btree ("collectedCardId");


CREATE UNIQUE INDEX "iX$tag$collectedCard_TagId_StackId_UserId" ON public."tag$collectedCard" USING btree ("tagId", "stackId", "userId");


CREATE UNIQUE INDEX "iX$tag$name" ON public."tag" USING btree (upper(("name")::text));


CREATE INDEX "iX$tag$user_CollateInstance_DefaultTagId" ON public."tag$user$collateInstance" USING btree ("defaultTagId");


CREATE INDEX "iX$user$collateInstance_CollateInstanceId" ON public."user$collateInstance" USING btree ("collateInstanceId");


CREATE INDEX "iX$user$collateInstance_DefaultCardSettingId" ON public."user$collateInstance" USING btree ("defaultCardSettingId");


CREATE INDEX "iX$vote$commentCollate_UserId" ON public."vote$commentCollate" USING btree ("userId");


CREATE INDEX "iX$vote$commentStack_UserId" ON public."vote$commentStack" USING btree ("userId");


CREATE INDEX "iX$vote$feedback_UserId" ON public."vote$feedback" USING btree ("userId");


CREATE UNIQUE INDEX "uQ$branch$stackId_Name" ON public."branch" USING btree ("stackId", upper(("name")::text));


CREATE UNIQUE INDEX "uQ$deck$userId_Name" ON public."deck" USING btree ("userId", upper(("name")::text));


CREATE INDEX idx_fts_branchinstance_tsvector ON public."branchInstance" USING gin ("tsVector");


CREATE INDEX idx_fts_collateinstance_tsvector ON public."collateInstance" USING gin ("tsVector");


CREATE INDEX idx_fts_communalfieldinstance_tsvector ON public."communalFieldInstance" USING gin ("tsVector");


CREATE INDEX idx_fts_relationship_tsvector ON public."relationship" USING gin ("tsVector");


CREATE INDEX idx_fts_tag_tsvector ON public."tag" USING gin ("tsVector");


CREATE INDEX idx_fts_user_tsvector ON public."user" USING gin ("tsVector");


CREATE CONSTRAINT TRIGGER ctr_branch_insertupdate AFTER INSERT OR UPDATE ON public."branch" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.fn_ctr_branch_insertupdate();


CREATE CONSTRAINT TRIGGER ctr_collectedcard_insertupdate AFTER INSERT OR UPDATE ON public."collectedCard" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.fn_ctr_collectedcard_insertupdate();


CREATE TRIGGER tr_branch_afterinsertupdate AFTER INSERT OR UPDATE ON public."branch" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_branch_afterinsertupdate();


CREATE TRIGGER tr_branchinstance_beforeinsert BEFORE INSERT ON public."branchInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_branchinstance_beforeinsert();


CREATE TRIGGER tr_collateinstance_beforeinsert BEFORE INSERT ON public."collateInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_collateinstance_beforeinsert();


CREATE TRIGGER tr_collectedcard_afterinsertdeleteupdate AFTER INSERT OR DELETE OR UPDATE ON public."collectedCard" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_collectedcard_afterinsertdeleteupdate();


CREATE TRIGGER tr_collectedcard_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."collectedCard" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_collectedcard_beforeinsertupdate();


CREATE TRIGGER tr_communalfieldinstance_beforeinsert BEFORE INSERT ON public."communalFieldInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_communalfieldinstance_beforeinsert();


CREATE TRIGGER tr_deck_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."deck" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_deck_beforeinsertupdate();


CREATE TRIGGER tr_deckfollower_afterinsertdeleteupdate AFTER INSERT OR DELETE OR UPDATE ON public."deckFollowers" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_deckfollower_afterinsertdeleteupdate();


CREATE TRIGGER tr_relationship_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."relationship" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_relationship_beforeinsertupdate();


CREATE TRIGGER tr_tag_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."tag" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_tag_beforeinsertupdate();


CREATE TRIGGER tr_user_afterinsert AFTER INSERT ON public."user" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_user_afterinsert();


CREATE TRIGGER tr_user_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."user" FOR EACH ROW EXECUTE FUNCTION public.fn_tr_user_beforeinsertupdate();


ALTER TABLE ONLY public."branchInstance"
    ADD CONSTRAINT "fK$branchInstance$branch_BranchId" FOREIGN KEY ("branchId") REFERENCES public."branch"("Id");


ALTER TABLE ONLY public."branchInstance"
    ADD CONSTRAINT "fK$branchInstance$branch_StackId_BranchId" FOREIGN KEY ("stackId", "branchId") REFERENCES public."branch"("StackId", "id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."branchInstance"
    ADD CONSTRAINT "fK$branchInstance$collateInstance_CollateInstanceId" FOREIGN KEY ("collateInstanceId") REFERENCES public."collateInstance"("Id");


ALTER TABLE ONLY public."branch"
    ADD CONSTRAINT "fK$branch$branchInstance_LatestInstanceId" FOREIGN KEY ("latestInstanceId", "id") REFERENCES public."branchInstance"("Id", "branchId") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."branch"
    ADD CONSTRAINT "fK$branch$stack_StackId" FOREIGN KEY ("stackId") REFERENCES public."stack"("Id");


ALTER TABLE ONLY public."branch"
    ADD CONSTRAINT "fK$branch$user_AuthorId" FOREIGN KEY ("authorId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."cardSetting"
    ADD CONSTRAINT "fK$cardSetting$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."collateInstance"
    ADD CONSTRAINT "fK$collateInstance$collate_CollateId" FOREIGN KEY ("collateId") REFERENCES public."collate"("Id");


ALTER TABLE ONLY public."collate"
    ADD CONSTRAINT "fK$collate$collateInstance_LatestInstanceId" FOREIGN KEY ("latestInstanceId", "id") REFERENCES public."collateInstance"("Id", "collateId") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."collate"
    ADD CONSTRAINT "fK$collate$user_AuthorId" FOREIGN KEY ("authorId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$branchInstance_BranchInstanceId" FOREIGN KEY ("branchInstanceId") REFERENCES public."branchInstance"("Id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$branchInstance_BranchInstanceId_BranchId" FOREIGN KEY ("branchId", "branchInstanceId") REFERENCES public."branchInstance"("BranchId", "id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$branch_BranchId" FOREIGN KEY ("branchId") REFERENCES public."branch"("Id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$branch_BranchId_StackId" FOREIGN KEY ("stackId", "branchId") REFERENCES public."branch"("StackId", "id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$cardSetting_CardSettingId" FOREIGN KEY ("cardSettingId") REFERENCES public."cardSetting"("Id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$deck_DeckId" FOREIGN KEY ("deckId") REFERENCES public."deck"("Id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$stack_StackId" FOREIGN KEY ("stackId") REFERENCES public."stack"("Id");


ALTER TABLE ONLY public."collectedCard"
    ADD CONSTRAINT "fK$collectedCard$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."commentCollate"
    ADD CONSTRAINT "fK$commentCollate$collate_CollateId" FOREIGN KEY ("collateId") REFERENCES public."collate"("Id");


ALTER TABLE ONLY public."commentCollate"
    ADD CONSTRAINT "fK$commentCollate$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."commentStack"
    ADD CONSTRAINT "fK$commentStack$stack_StackId" FOREIGN KEY ("stackId") REFERENCES public."stack"("Id");


ALTER TABLE ONLY public."commentStack"
    ADD CONSTRAINT "fK$commentStack$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."communalFieldInstance$branchInstance"
    ADD CONSTRAINT "fK$communalFieldInstance$branchInstance_BranchInstanceId" FOREIGN KEY ("branchInstanceId") REFERENCES public."branchInstance"("Id");


ALTER TABLE ONLY public."communalFieldInstance$branchInstance"
    ADD CONSTRAINT "fK$communalFieldInstance$branchInstance_CommunalFieldInstanceId" FOREIGN KEY ("communalFieldInstanceId") REFERENCES public."communalFieldInstance"("Id");


ALTER TABLE ONLY public."communalFieldInstance"
    ADD CONSTRAINT "fK$communalFieldInstance$communalField_CommunalFieldId" FOREIGN KEY ("communalFieldId") REFERENCES public."communalField"("Id");


ALTER TABLE ONLY public."communalField"
    ADD CONSTRAINT "fK$communalField$communalFieldInstance_LatestInstanceId" FOREIGN KEY ("latestInstanceId", "id") REFERENCES public."communalFieldInstance"("Id", "communalFieldId") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."communalField"
    ADD CONSTRAINT "fK$communalField$user_AuthorId" FOREIGN KEY ("authorId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."deckFollowers"
    ADD CONSTRAINT "fK$deckFollowers$deckId" FOREIGN KEY ("deckId") REFERENCES public."deck"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."deckFollowers"
    ADD CONSTRAINT "fK$deckFollowers$followerId" FOREIGN KEY ("followerId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."deck"
    ADD CONSTRAINT "fK$deck$deck_SourceId" FOREIGN KEY ("sourceId") REFERENCES public."deck"("Id") ON DELETE SET NULL;


ALTER TABLE ONLY public."deck"
    ADD CONSTRAINT "fK$deck$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."feedback"
    ADD CONSTRAINT "fK$feedback$feedback_ParentId" FOREIGN KEY ("parentId") REFERENCES public."feedback"("Id");


ALTER TABLE ONLY public."feedback"
    ADD CONSTRAINT "fK$feedback$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."file$branchInstance"
    ADD CONSTRAINT "fK$file$branchInstance_BranchInstance_BranchInstanceId" FOREIGN KEY ("branchInstanceId") REFERENCES public."branchInstance"("Id");


ALTER TABLE ONLY public."file$branchInstance"
    ADD CONSTRAINT "fK$file$branchInstance_File_FileId" FOREIGN KEY ("fileId") REFERENCES public."file"("Id");


ALTER TABLE ONLY public."filter"
    ADD CONSTRAINT "fK$filter$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."history"
    ADD CONSTRAINT "fK$history$branchInstance_BranchInstanceId" FOREIGN KEY ("branchInstanceId") REFERENCES public."branchInstance"("Id");


ALTER TABLE ONLY public."history"
    ADD CONSTRAINT "fK$history$collectedCard_CollectedCardId" FOREIGN KEY ("collectedCardId") REFERENCES public."collectedCard"("Id") ON DELETE SET NULL;


ALTER TABLE ONLY public."history"
    ADD CONSTRAINT "fK$history$user_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$branchInstance_BranchInstanceId" FOREIGN KEY ("branchInstanceId") REFERENCES public."branchInstance"("Id");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$branchInstance_BranchInstanceId_BranchId" FOREIGN KEY ("branchInstanceId", "branchId") REFERENCES public."branchInstance"("Id", "branchId");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$branchInstance_BranchInstanceId_StackId" FOREIGN KEY ("branchInstanceId", "stackId") REFERENCES public."branchInstance"("Id", "stackId");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$branch_BranchId" FOREIGN KEY ("branchId") REFERENCES public."branch"("Id");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$branch_BranchId_StackId" FOREIGN KEY ("branchId", "stackId") REFERENCES public."branch"("Id", "stackId");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$collateInstance_CollateInstanceId" FOREIGN KEY ("collateInstanceId") REFERENCES public."collateInstance"("Id");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$collate_CollateId" FOREIGN KEY ("collateId") REFERENCES public."collate"("Id");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$deck_DeckId" FOREIGN KEY ("deckId") REFERENCES public."deck"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$stack_StackId" FOREIGN KEY ("stackId") REFERENCES public."stack"("Id");


ALTER TABLE ONLY public."notification"
    ADD CONSTRAINT "fK$notification$user_SenderId" FOREIGN KEY ("senderId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."receivedNotification"
    ADD CONSTRAINT "fK$receivedNotification$user_NotificationId" FOREIGN KEY ("notificationId") REFERENCES public."notification"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."receivedNotification"
    ADD CONSTRAINT "fK$receivedNotification$user_ReceiverId" FOREIGN KEY ("receiverId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."relationship$collectedCard"
    ADD CONSTRAINT "fK$relationship$collectedCard_CollectedCard_SourceCollectedCard" FOREIGN KEY ("sourceCollectedCardId") REFERENCES public."collectedCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."relationship$collectedCard"
    ADD CONSTRAINT "fK$relationship$collectedCard_CollectedCard_TargetCollectedCard" FOREIGN KEY ("targetCollectedCardId") REFERENCES public."collectedCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."relationship$collectedCard"
    ADD CONSTRAINT "fK$relationship$collectedCard_Relationship_RelationshipId" FOREIGN KEY ("relationshipId") REFERENCES public."relationship"("Id");


ALTER TABLE ONLY public."relationship$collectedCard"
    ADD CONSTRAINT "fK$relationship$collectedCard_SourceCollectedCard_UserId_StackI" FOREIGN KEY ("sourceCollectedCardId", "userId", "sourceStackId") REFERENCES public."collectedCard"("Id", "userId", "stackId");


ALTER TABLE ONLY public."relationship$collectedCard"
    ADD CONSTRAINT "fK$relationship$collectedCard_TargetCollectedCard_UserId_StackI" FOREIGN KEY ("targetCollectedCardId", "userId", "targetStackId") REFERENCES public."collectedCard"("Id", "userId", "stackId");


ALTER TABLE ONLY public."stack"
    ADD CONSTRAINT "fK$stack$branchInstance_CopySourceId" FOREIGN KEY ("copySourceId") REFERENCES public."branchInstance"("Id");


ALTER TABLE ONLY public."stack"
    ADD CONSTRAINT "fK$stack$branch_DefaultBranchId" FOREIGN KEY ("defaultBranchId", "id") REFERENCES public."branch"("Id", "stackId") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."stack"
    ADD CONSTRAINT "fK$stack$user_AuthorId" FOREIGN KEY ("authorId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."tag$collectedCard"
    ADD CONSTRAINT "fK$tag$collectedCard_CollectedCardId_UserId_StackId" FOREIGN KEY ("collectedCardId", "userId", "stackId") REFERENCES public."collectedCard"("Id", "userId", "stackId") ON DELETE CASCADE;


ALTER TABLE ONLY public."tag$collectedCard"
    ADD CONSTRAINT "fK$tag$collectedCard_CollectedCard_CollectedCardId" FOREIGN KEY ("collectedCardId") REFERENCES public."collectedCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."tag$collectedCard"
    ADD CONSTRAINT "fK$tag$collectedCard_Tag_TagId" FOREIGN KEY ("tagId") REFERENCES public."tag"("Id");


ALTER TABLE ONLY public."tag$user$collateInstance"
    ADD CONSTRAINT "fK$tag$user_CollateInstance_Tag_DefaultTagId" FOREIGN KEY ("defaultTagId") REFERENCES public."tag"("Id");


ALTER TABLE ONLY public."tag$user$collateInstance"
    ADD CONSTRAINT "fK$tag$user_TemplatInst_User_TemplatInst_UserId_TemplatInstId" FOREIGN KEY ("userId", "collateInstanceId") REFERENCES public."user$collateInstance"("UserId", "collateInstanceId");


ALTER TABLE ONLY public."user"
    ADD CONSTRAINT "fK$user$cardSetting_DefaultCardSettingId" FOREIGN KEY ("defaultCardSettingId", "id") REFERENCES public."cardSetting"("Id", "userId") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."user$collateInstance"
    ADD CONSTRAINT "fK$user$collateInstance_CardSetting_DefaultCardSettingId" FOREIGN KEY ("defaultCardSettingId") REFERENCES public."cardSetting"("Id");


ALTER TABLE ONLY public."user$collateInstance"
    ADD CONSTRAINT "fK$user$collateInstance_CollateInstance_CollateInstanceId" FOREIGN KEY ("collateInstanceId") REFERENCES public."collateInstance"("Id");


ALTER TABLE ONLY public."user$collateInstance"
    ADD CONSTRAINT "fK$user$collateInstance_User_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."user"
    ADD CONSTRAINT "fK$user$deck_DefaultDeckId" FOREIGN KEY ("defaultDeckId", "id") REFERENCES public."deck"("Id", "userId") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."vote$commentCollate"
    ADD CONSTRAINT "fK$vote$commentCollate_CommentCollate_CommentCollateId" FOREIGN KEY ("commentCollateId") REFERENCES public."commentCollate"("Id");


ALTER TABLE ONLY public."vote$commentCollate"
    ADD CONSTRAINT "fK$vote$commentCollate_User_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."vote$commentStack"
    ADD CONSTRAINT "fK$vote$commentStack_CommentStack_CommentStackId" FOREIGN KEY ("commentStackId") REFERENCES public."commentStack"("Id");


ALTER TABLE ONLY public."vote$commentStack"
    ADD CONSTRAINT "fK$vote$commentStack_User_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");


ALTER TABLE ONLY public."vote$feedback"
    ADD CONSTRAINT "fK$vote$feedback_Feedback_FeedbackId" FOREIGN KEY ("feedbackId") REFERENCES public."feedback"("Id");


ALTER TABLE ONLY public."vote$feedback"
    ADD CONSTRAINT "fK$vote$feedback_User_UserId" FOREIGN KEY ("userId") REFERENCES public."user"("Id");



