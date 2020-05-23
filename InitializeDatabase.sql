-- medTODO counts involving `"CardState" <> 3` are going to be slightly wrong. They're using AcquiredCard, and a Card can have multiple AcquiredCards.

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

CREATE FUNCTION public.fn_acquiredcard_afterinsertdeleteupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF (TG_OP = 'INSERT' OR TG_OP = 'UPDATE') THEN
            IF (1 < (SELECT COUNT(*) FROM (SELECT DISTINCT ac."BranchInstanceId" FROM "AcquiredCard" ac WHERE ac."UserId" = NEW."UserId" AND ac."CardId" = NEW."CardId") _)) THEN
                RAISE EXCEPTION 'UserId #% with AcquiredCard #% tried to have BranchInstanceId #%, but they already have BranchInstanceId #%', (NEW."UserId"), (NEW."Id"), (NEW."BranchInstanceId"), (SELECT ac."BranchInstanceId" FROM "AcquiredCard" ac WHERE ac."UserId" = 3 AND ac."CardId" = NEW."CardId" LIMIT 1);
            END IF;
        END IF;
		IF (TG_OP = 'DELETE' OR (TG_OP = 'UPDATE' AND (OLD."BranchInstanceId" <> NEW."BranchInstanceId" OR OLD."CardState" <> NEW."CardState"))) THEN
            UPDATE	"BranchInstance" ci
            SET     "Users" = ( SELECT  COUNT(*) FROM
                                  ( SELECT DISTINCT ac."CardId", ac."UserId"
                                    FROM    "AcquiredCard" ac
                                    WHERE   "BranchInstanceId" = OLD."BranchInstanceId" AND "CardState" <> 3 )_)
            WHERE	ci."Id" = OLD."BranchInstanceId";
            UPDATE	"Branch" b
            SET		"Users" = ( SELECT  COUNT(*) FROM
                                  ( SELECT DISTINCT ac."CardId", ac."UserId"
                                    FROM    "AcquiredCard" ac
                                    WHERE   "BranchId" = OLD."BranchId" AND "CardState" <> 3 )_)
            WHERE	b."Id" = OLD."BranchId";
            UPDATE  "Card" card
            SET     "Users" = ( SELECT  COUNT(*) FROM
                                  ( SELECT DISTINCT c."Id", ac."UserId"
                                    FROM    "Card" c
                                    JOIN    "AcquiredCard" ac on ac."CardId" = c."Id"
                                    WHERE   ac."CardState" <> 3 AND ac."CardId" = OLD."CardId")_)
            WHERE card."Id" = OLD."CardId";
        END IF;
        IF (TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND (OLD."BranchInstanceId" <> NEW."BranchInstanceId" OR OLD."CardState" <> NEW."CardState"))) THEN
            UPDATE	"BranchInstance" ci
            SET     "Users" = ( SELECT  COUNT(*) FROM
                                  ( SELECT DISTINCT ac."CardId", ac."UserId"
                                    FROM    "AcquiredCard" ac
                                    WHERE   "BranchInstanceId" = NEW."BranchInstanceId" AND "CardState" <> 3 )_)
            WHERE	ci."Id" = NEW."BranchInstanceId";
            UPDATE	"Branch" b
            SET		"Users" = ( SELECT  COUNT(*) FROM
                                  ( SELECT DISTINCT ac."CardId", ac."UserId"
                                    FROM    "AcquiredCard" ac
                                    WHERE   "BranchId" = NEW."BranchId" AND "CardState" <> 3 )_)
            WHERE	b."Id" = NEW."BranchId";
            UPDATE  "Card" card
            SET     "Users" = ( SELECT  COUNT(*) FROM
                                  ( SELECT DISTINCT c."Id", ac."UserId"
                                    FROM    "Card" c
                                    JOIN    "AcquiredCard" ac on ac."CardId" = c."Id"
                                    WHERE   ac."CardState" <> 3 AND ac."CardId" = NEW."CardId")_)
            WHERE card."Id" = NEW."CardId";
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_acquiredcard_afterinsertdeleteupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_acquiredcard_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
		IF (TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND (OLD."BranchInstanceId" <> NEW."BranchInstanceId" OR OLD."Index" <> NEW."Index"))) THEN
		IF ((SELECT bi."MaxIndexInclusive" FROM public."BranchInstance" bi WHERE bi."Id" = NEW."BranchInstanceId") < NEW."Index") THEN
			RAISE EXCEPTION 'UserId #% with AcquiredCard #% tried to have index %, which exceeds the MaxIndexInclusive value of % on its BranchInstanceId #%', (NEW."UserId"), (NEW."Id"), (NEW."Index"), (SELECT bi."MaxIndexInclusive" FROM public."BranchInstance" bi WHERE bi."Id" = NEW."BranchInstanceId"), (NEW."BranchInstanceId");
		END IF;
		END IF;
        IF (NEW."TsVectorHelper" IS NOT NULL) THEN
            NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."TsVectorHelper");
            NEW."TsVectorHelper" = NULL;
        END IF;
        RETURN NEW;
    END;
$$;


ALTER FUNCTION public.fn_acquiredcard_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_branch_afterinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    DECLARE
        default_branch_id integer NOT NULL := 0;
    BEGIN
        IF (TG_OP = 'INSERT') THEN
            UPDATE "Card" c
            SET    "DefaultBranchId" = (NEW."Id")
            WHERE (c."Id" = NEW."CardId" AND c."DefaultBranchId" = 0);
        END IF;
        
        default_branch_id := (SELECT "DefaultBranchId" FROM "Card" c WHERE NEW."CardId" = c."Id");
        
        IF ((NEW."Name" IS NOT NULL) AND (default_branch_id = NEW."Id")) THEN
            RAISE EXCEPTION 'Default Branches must have a null Name. CardId#% with BranchId#% by UserId#% just attempted to be titled "%"', (NEW."CardId"), (NEW."Id"), (NEW."AuthorId"), (NEW."Name");
        ELSIF ((NEW."Name" IS NULL) AND (default_branch_id <> NEW."Id")) THEN
            RAISE EXCEPTION 'Only Default Branches may have a null Name. CardId#% with BranchId#% by UserId#% just attempted to be titled "%"', (NEW."CardId"), (NEW."Id"), (NEW."AuthorId"), (NEW."Name");
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_branch_afterinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_branchinstance_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE "Branch" b
  SET    "LatestInstanceId" = NEW."Id"
  WHERE  b."Id" = NEW."BranchId";
  IF (NEW."TsVectorHelper" IS NOT NULL) THEN
    NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."TsVectorHelper");
    NEW."TsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_branchinstance_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_collateinstance_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE "Collate" t
  SET    "LatestInstanceId" = NEW."Id"
  WHERE  t."Id" = NEW."CollateId";
  IF (NEW."CWeightTsVectorHelper" IS NOT NULL) THEN
    NEW."TsVector" =
        setweight(to_tsvector('pg_catalog.english', NEW."Name"), 'A') ||
        setweight(to_tsvector('pg_catalog.english', NEW."CWeightTsVectorHelper"), 'C') ||
        setweight(to_tsvector('pg_catalog.english', NEW."Css"), 'D');
    NEW."CWeightTsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_collateinstance_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_communalfieldinstance_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE "CommunalField" cf
  SET    "LatestInstanceId" = NEW."Id"
  WHERE  cf."Id" = NEW."CommunalFieldId";
  IF (NEW."BWeightTsVectorHelper" IS NOT NULL) THEN
    NEW."TsVector" =
        setweight(to_tsvector('pg_catalog.english', NEW."FieldName"), 'A') ||
        setweight(to_tsvector('pg_catalog.english', NEW."BWeightTsVectorHelper"), 'B');
    NEW."BWeightTsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.fn_communalfieldinstance_beforeinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_deck_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."Name");
  return NEW;
end
$$;


ALTER FUNCTION public.fn_deck_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_relationship_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
begin
  NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."Name");
  return NEW;
end
$$;


ALTER FUNCTION public.fn_relationship_beforeinsertupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_tag_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."Name");
  return NEW;
end
$$;


ALTER FUNCTION public.fn_tag_beforeinsertupdate() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE TABLE public."AcquiredCard" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "CardId" integer NOT NULL,
    "BranchId" integer NOT NULL,
    "BranchInstanceId" integer NOT NULL,
    "Index" smallint NOT NULL,
    "CardState" smallint NOT NULL,
    "EaseFactorInPermille" smallint NOT NULL,
    "IntervalOrStepsIndex" smallint NOT NULL,
    "Due" timestamp without time zone NOT NULL,
    "CardSettingId" integer NOT NULL,
    "DeckId" integer,
    "IsLapsed" boolean NOT NULL,
    "PersonalField" text NOT NULL,
    "TsVectorHelper" text,
    "TsVector" tsvector,
    CONSTRAINT "AcquiredCard_TsVectorHelper_IsNull" CHECK (("TsVectorHelper" IS NULL))
);


ALTER TABLE public."AcquiredCard" OWNER TO postgres;

CREATE TABLE public."Branch" (
    "Id" integer NOT NULL,
    "Name" character varying(64),
    "AuthorId" integer NOT NULL,
    "CardId" integer NOT NULL,
    "LatestInstanceId" integer NOT NULL,
    "Users" integer NOT NULL,
    "IsListed" boolean NOT NULL
);


ALTER TABLE public."Branch" OWNER TO postgres;

CREATE VIEW public."AcquiredCardIsLatest" AS
 SELECT a."Id",
    a."UserId",
    a."CardId",
    a."BranchId",
    a."BranchInstanceId",
    a."Index",
    a."CardState",
    a."EaseFactorInPermille",
    a."IntervalOrStepsIndex",
    a."Due",
    a."CardSettingId",
    a."IsLapsed",
    a."PersonalField",
    (b."LatestInstanceId" IS NULL) AS "IsLatest"
   FROM (public."AcquiredCard" a
     LEFT JOIN public."Branch" b ON ((b."LatestInstanceId" = a."BranchInstanceId")));


ALTER TABLE public."AcquiredCardIsLatest" OWNER TO postgres;

ALTER TABLE public."AcquiredCard" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."AcquiredCard_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."AlphaBetaKey" (
    "Id" integer NOT NULL,
    "Key" character varying(50) NOT NULL,
    "IsUsed" boolean NOT NULL
);


ALTER TABLE public."AlphaBetaKey" OWNER TO postgres;

ALTER TABLE public."AlphaBetaKey" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."AlphaBetaKey_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."BranchInstance" (
    "Id" integer NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "Modified" timestamp without time zone,
    "CardId" integer NOT NULL,
    "BranchId" integer NOT NULL,
    "IsDmca" boolean NOT NULL,
    "FieldValues" text NOT NULL,
    "CollateInstanceId" integer NOT NULL,
    "Users" integer NOT NULL,
    "EditSummary" character varying(200) NOT NULL,
    "AnkiNoteId" bigint,
    "Hash" bit(512) NOT NULL,
    "TsVectorHelper" text,
    "TsVector" tsvector,
    "MaxIndexInclusive" smallint NOT NULL,
    CONSTRAINT "BranchInstance_TsVectorHelper_IsNull" CHECK (("TsVectorHelper" IS NULL))
);


ALTER TABLE public."BranchInstance" OWNER TO postgres;

CREATE TABLE public."Relationship" (
    "Id" integer NOT NULL,
    "Name" character varying(250) NOT NULL,
    "TsVector" tsvector
);


ALTER TABLE public."Relationship" OWNER TO postgres;

CREATE TABLE public."Relationship_AcquiredCard" (
    "RelationshipId" integer NOT NULL,
    "UserId" integer NOT NULL,
    "SourceCardId" integer NOT NULL,
    "TargetCardId" integer NOT NULL,
    "SourceAcquiredCardId" integer NOT NULL,
    "TargetAcquiredCardId" integer NOT NULL
);


ALTER TABLE public."Relationship_AcquiredCard" OWNER TO postgres;

CREATE VIEW public."BranchInstanceRelationshipCount" AS
 SELECT sac."BranchInstanceId" AS "SourceBranchInstanceId",
    tac."BranchInstanceId" AS "TargetBranchInstanceId",
    unnest(ARRAY[sac."BranchInstanceId", tac."BranchInstanceId"]) AS "BranchInstanceId",
    ( SELECT r."Name"
           FROM public."Relationship" r
          WHERE (r."Id" = rac."RelationshipId")
         LIMIT 1) AS "Name",
    count(*) AS "Count"
   FROM ((public."Relationship_AcquiredCard" rac
     JOIN public."AcquiredCard" sac ON ((rac."SourceAcquiredCardId" = sac."Id")))
     JOIN public."AcquiredCard" tac ON ((rac."TargetAcquiredCardId" = tac."Id")))
  WHERE ((sac."CardState" <> 3) AND (tac."CardState" <> 3))
  GROUP BY sac."BranchInstanceId", tac."BranchInstanceId", rac."RelationshipId";


ALTER TABLE public."BranchInstanceRelationshipCount" OWNER TO postgres;

CREATE TABLE public."Tag" (
    "Id" integer NOT NULL,
    "Name" character varying(250) NOT NULL,
    "TsVector" tsvector
);


ALTER TABLE public."Tag" OWNER TO postgres;

CREATE TABLE public."Tag_AcquiredCard" (
    "TagId" integer NOT NULL,
    "UserId" integer NOT NULL,
    "CardId" integer NOT NULL,
    "AcquiredCardId" integer NOT NULL
);


ALTER TABLE public."Tag_AcquiredCard" OWNER TO postgres;

CREATE VIEW public."BranchInstanceTagCount" AS
 SELECT i."Id" AS "BranchInstanceId",
    ( SELECT t."Name"
           FROM public."Tag" t
          WHERE (t."Id" = ta."TagId")
         LIMIT 1) AS "Name",
    count(*) AS "Count"
   FROM ((public."BranchInstance" i
     JOIN public."AcquiredCard" ac ON ((ac."BranchInstanceId" = i."Id")))
     JOIN public."Tag_AcquiredCard" ta ON ((ta."AcquiredCardId" = ac."Id")))
  WHERE (ac."CardState" <> 3)
  GROUP BY i."Id", ta."TagId";


ALTER TABLE public."BranchInstanceTagCount" OWNER TO postgres;

ALTER TABLE public."BranchInstance" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."BranchInstance_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."Branch" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Branch_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."Card" (
    "Id" integer NOT NULL,
    "AuthorId" integer NOT NULL,
    "Users" integer NOT NULL,
    "CopySourceId" integer,
    "DefaultBranchId" integer NOT NULL,
    "IsListed" boolean NOT NULL
);


ALTER TABLE public."Card" OWNER TO postgres;

CREATE VIEW public."CardRelationshipCount" AS
 SELECT sac."CardId" AS "SourceCardId",
    tac."CardId" AS "TargetCardId",
    unnest(ARRAY[sac."CardId", tac."CardId"]) AS "CardId",
    ( SELECT r."Name"
           FROM public."Relationship" r
          WHERE (r."Id" = rac."RelationshipId")
         LIMIT 1) AS "Name",
    count(*) AS "Count"
   FROM ((public."Relationship_AcquiredCard" rac
     JOIN public."AcquiredCard" sac ON ((rac."SourceAcquiredCardId" = sac."Id")))
     JOIN public."AcquiredCard" tac ON ((rac."TargetAcquiredCardId" = tac."Id")))
  WHERE ((sac."CardState" <> 3) AND (tac."CardState" <> 3))
  GROUP BY sac."CardId", tac."CardId", rac."RelationshipId";


ALTER TABLE public."CardRelationshipCount" OWNER TO postgres;

CREATE TABLE public."CardSetting" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "NewCardsStepsInMinutes" character varying(100) NOT NULL,
    "NewCardsMaxPerDay" smallint NOT NULL,
    "NewCardsGraduatingIntervalInDays" smallint NOT NULL,
    "NewCardsEasyIntervalInDays" smallint NOT NULL,
    "NewCardsStartingEaseFactorInPermille" smallint NOT NULL,
    "NewCardsBuryRelated" boolean NOT NULL,
    "MatureCardsMaxPerDay" smallint NOT NULL,
    "MatureCardsEaseFactorEasyBonusFactorInPermille" smallint NOT NULL,
    "MatureCardsIntervalFactorInPermille" smallint NOT NULL,
    "MatureCardsMaximumIntervalInDays" smallint NOT NULL,
    "MatureCardsHardIntervalFactorInPermille" smallint NOT NULL,
    "MatureCardsBuryRelated" boolean NOT NULL,
    "LapsedCardsStepsInMinutes" character varying(100) NOT NULL,
    "LapsedCardsNewIntervalFactorInPermille" smallint NOT NULL,
    "LapsedCardsMinimumIntervalInDays" smallint NOT NULL,
    "LapsedCardsLeechThreshold" smallint NOT NULL,
    "ShowAnswerTimer" boolean NOT NULL,
    "AutomaticallyPlayAudio" boolean NOT NULL,
    "ReplayQuestionAudioOnAnswer" boolean NOT NULL
);


ALTER TABLE public."CardSetting" OWNER TO postgres;

ALTER TABLE public."CardSetting" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CardSetting_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE VIEW public."CardTagCount" AS
 SELECT c."Id" AS "CardId",
    ( SELECT t."Name"
           FROM public."Tag" t
          WHERE (t."Id" = ta."TagId")
         LIMIT 1) AS "Name",
    count(*) AS "Count"
   FROM ((public."Card" c
     JOIN public."AcquiredCard" ac ON ((ac."CardId" = c."Id")))
     JOIN public."Tag_AcquiredCard" ta ON ((ta."AcquiredCardId" = ac."Id")))
  WHERE (ac."CardState" <> 3)
  GROUP BY c."Id", ta."TagId";


ALTER TABLE public."CardTagCount" OWNER TO postgres;

ALTER TABLE public."Card" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Card_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."Collate" (
    "Id" integer NOT NULL,
    "AuthorId" integer NOT NULL,
    "LatestInstanceId" integer NOT NULL,
    "IsListed" boolean NOT NULL
);


ALTER TABLE public."Collate" OWNER TO postgres;

CREATE TABLE public."CollateInstance" (
    "Id" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "CollateId" integer NOT NULL,
    "Css" character varying(4000) NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "Modified" timestamp without time zone,
    "LatexPre" character varying(500) NOT NULL,
    "LatexPost" character varying(500) NOT NULL,
    "IsDmca" boolean NOT NULL,
    "Templates" text NOT NULL,
    "Type" smallint NOT NULL,
    "Fields" character varying(4000) NOT NULL,
    "EditSummary" character varying(200) NOT NULL,
    "AnkiId" bigint,
    "Hash" bit(512) NOT NULL,
    "CWeightTsVectorHelper" text,
    "TsVector" tsvector,
    CONSTRAINT "CollateInstance_CWeightTsVectorHelper_IsNull" CHECK (("CWeightTsVectorHelper" IS NULL))
);


ALTER TABLE public."CollateInstance" OWNER TO postgres;

ALTER TABLE public."CollateInstance" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CollateInstance_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."Collate" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Collate_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."CommentCard" (
    "Id" integer NOT NULL,
    "CardId" integer NOT NULL,
    "UserId" integer NOT NULL,
    "Text" character varying(500) NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "IsDmca" boolean NOT NULL
);


ALTER TABLE public."CommentCard" OWNER TO postgres;

ALTER TABLE public."CommentCard" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CommentCard_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."CommentCollate" (
    "Id" integer NOT NULL,
    "CollateId" integer NOT NULL,
    "UserId" integer NOT NULL,
    "Text" character varying(500) NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "IsDmca" boolean NOT NULL
);


ALTER TABLE public."CommentCollate" OWNER TO postgres;

ALTER TABLE public."CommentCollate" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CommentCollate_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."CommunalField" (
    "Id" integer NOT NULL,
    "AuthorId" integer NOT NULL,
    "LatestInstanceId" integer NOT NULL,
    "IsListed" boolean NOT NULL
);


ALTER TABLE public."CommunalField" OWNER TO postgres;

CREATE TABLE public."CommunalFieldInstance" (
    "Id" integer NOT NULL,
    "CommunalFieldId" integer NOT NULL,
    "FieldName" character varying(200) NOT NULL,
    "Value" text NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "Modified" timestamp without time zone,
    "EditSummary" character varying(200) NOT NULL,
    "BWeightTsVectorHelper" text,
    "TsVector" tsvector,
    CONSTRAINT "CommunalFieldInstance_BWeightTsVectorHelper_IsNull" CHECK (("BWeightTsVectorHelper" IS NULL))
);


ALTER TABLE public."CommunalFieldInstance" OWNER TO postgres;

CREATE TABLE public."CommunalFieldInstance_BranchInstance" (
    "BranchInstanceId" integer NOT NULL,
    "CommunalFieldInstanceId" integer NOT NULL
);


ALTER TABLE public."CommunalFieldInstance_BranchInstance" OWNER TO postgres;

ALTER TABLE public."CommunalFieldInstance" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CommunalFieldInstance_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."CommunalField" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CommunalField_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."Deck" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "Name" character varying(250) NOT NULL,
    "IsPublic" boolean NOT NULL,
    "TsVector" tsvector
);


ALTER TABLE public."Deck" OWNER TO postgres;

ALTER TABLE public."Deck" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Deck_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."Feedback" (
    "Id" integer NOT NULL,
    "Title" character varying(50) NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "UserId" integer NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "ParentId" integer,
    "Priority" smallint
);


ALTER TABLE public."Feedback" OWNER TO postgres;

ALTER TABLE public."Feedback" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Feedback_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."File" (
    "Id" integer NOT NULL,
    "FileName" character varying(200) NOT NULL,
    "Data" bytea NOT NULL,
    "Sha256" bytea NOT NULL
);


ALTER TABLE public."File" OWNER TO postgres;

CREATE TABLE public."File_BranchInstance" (
    "BranchInstanceId" integer NOT NULL,
    "FileId" integer NOT NULL
);


ALTER TABLE public."File_BranchInstance" OWNER TO postgres;

ALTER TABLE public."File" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."File_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."Filter" (
    "Id" integer NOT NULL,
    "Name" character varying(128) NOT NULL,
    "UserId" integer NOT NULL,
    "Query" character varying(256) NOT NULL
);


ALTER TABLE public."Filter" OWNER TO postgres;

ALTER TABLE public."Filter" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Filter_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."History" (
    "Id" integer NOT NULL,
    "AcquiredCardId" integer NOT NULL,
    "Score" smallint NOT NULL,
    "Timestamp" timestamp without time zone NOT NULL,
    "IntervalWithUnusedStepsIndex" smallint NOT NULL,
    "EaseFactorInPermille" smallint NOT NULL,
    "TimeFromSeeingQuestionToScoreInSecondsPlus32768" smallint NOT NULL
);


ALTER TABLE public."History" OWNER TO postgres;

ALTER TABLE public."History" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."History_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."PotentialSignups" (
    "Id" integer NOT NULL,
    "Email" character varying(500) NOT NULL,
    "Message" character varying(1000) NOT NULL,
    "OneIsAlpha2Beta3Ga" smallint NOT NULL,
    "TimeStamp" timestamp without time zone NOT NULL
);


ALTER TABLE public."PotentialSignups" OWNER TO postgres;

ALTER TABLE public."PotentialSignups" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PotentialSignups_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."Relationship" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Relationship_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."Tag" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Tag_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."Tag_User_CollateInstance" (
    "UserId" integer NOT NULL,
    "CollateInstanceId" integer NOT NULL,
    "DefaultTagId" integer NOT NULL
);


ALTER TABLE public."Tag_User_CollateInstance" OWNER TO postgres;

CREATE TABLE public."User" (
    "Id" integer NOT NULL,
    "DisplayName" character varying(32) NOT NULL,
    "DefaultCardSettingId" integer,
    "ShowNextReviewTime" boolean NOT NULL,
    "ShowRemainingCardCount" boolean NOT NULL,
    "MixNewAndReview" smallint NOT NULL,
    "NextDayStartsAtXHoursPastMidnight" smallint NOT NULL,
    "LearnAheadLimitInMinutes" smallint NOT NULL,
    "TimeboxTimeLimitInMinutes" smallint NOT NULL,
    "IsNightMode" boolean NOT NULL
);


ALTER TABLE public."User" OWNER TO postgres;

CREATE TABLE public."User_CollateInstance" (
    "UserId" integer NOT NULL,
    "CollateInstanceId" integer NOT NULL,
    "DefaultCardSettingId" integer NOT NULL
);


ALTER TABLE public."User_CollateInstance" OWNER TO postgres;

ALTER TABLE public."User" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."User_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."Vote_CommentCard" (
    "CommentCardId" integer NOT NULL,
    "UserId" integer NOT NULL
);


ALTER TABLE public."Vote_CommentCard" OWNER TO postgres;

CREATE TABLE public."Vote_CommentCollate" (
    "CommentCollateId" integer NOT NULL,
    "UserId" integer NOT NULL
);


ALTER TABLE public."Vote_CommentCollate" OWNER TO postgres;

CREATE TABLE public."Vote_Feedback" (
    "FeedbackId" integer NOT NULL,
    "UserId" integer NOT NULL
);


ALTER TABLE public."Vote_Feedback" OWNER TO postgres;











INSERT INTO public."CardSetting" ("Id", "UserId", "Name", "NewCardsStepsInMinutes", "NewCardsMaxPerDay", "NewCardsGraduatingIntervalInDays", "NewCardsEasyIntervalInDays", "NewCardsStartingEaseFactorInPermille", "NewCardsBuryRelated", "MatureCardsMaxPerDay", "MatureCardsEaseFactorEasyBonusFactorInPermille", "MatureCardsIntervalFactorInPermille", "MatureCardsMaximumIntervalInDays", "MatureCardsHardIntervalFactorInPermille", "MatureCardsBuryRelated", "LapsedCardsStepsInMinutes", "LapsedCardsNewIntervalFactorInPermille", "LapsedCardsMinimumIntervalInDays", "LapsedCardsLeechThreshold", "ShowAnswerTimer", "AutomaticallyPlayAudio", "ReplayQuestionAudioOnAnswer") VALUES (1, 1, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public."CardSetting" ("Id", "UserId", "Name", "NewCardsStepsInMinutes", "NewCardsMaxPerDay", "NewCardsGraduatingIntervalInDays", "NewCardsEasyIntervalInDays", "NewCardsStartingEaseFactorInPermille", "NewCardsBuryRelated", "MatureCardsMaxPerDay", "MatureCardsEaseFactorEasyBonusFactorInPermille", "MatureCardsIntervalFactorInPermille", "MatureCardsMaximumIntervalInDays", "MatureCardsHardIntervalFactorInPermille", "MatureCardsBuryRelated", "LapsedCardsStepsInMinutes", "LapsedCardsNewIntervalFactorInPermille", "LapsedCardsMinimumIntervalInDays", "LapsedCardsLeechThreshold", "ShowAnswerTimer", "AutomaticallyPlayAudio", "ReplayQuestionAudioOnAnswer") VALUES (2, 2, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public."CardSetting" ("Id", "UserId", "Name", "NewCardsStepsInMinutes", "NewCardsMaxPerDay", "NewCardsGraduatingIntervalInDays", "NewCardsEasyIntervalInDays", "NewCardsStartingEaseFactorInPermille", "NewCardsBuryRelated", "MatureCardsMaxPerDay", "MatureCardsEaseFactorEasyBonusFactorInPermille", "MatureCardsIntervalFactorInPermille", "MatureCardsMaximumIntervalInDays", "MatureCardsHardIntervalFactorInPermille", "MatureCardsBuryRelated", "LapsedCardsStepsInMinutes", "LapsedCardsNewIntervalFactorInPermille", "LapsedCardsMinimumIntervalInDays", "LapsedCardsLeechThreshold", "ShowAnswerTimer", "AutomaticallyPlayAudio", "ReplayQuestionAudioOnAnswer") VALUES (3, 3, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);


INSERT INTO public."Collate" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (1, 2, 1001, true);
INSERT INTO public."Collate" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (2, 2, 1002, true);
INSERT INTO public."Collate" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (3, 2, 1003, true);
INSERT INTO public."Collate" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (4, 2, 1006, true);
INSERT INTO public."Collate" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (5, 2, 1007, true);


INSERT INTO public."CollateInstance" ("Id", "Name", "CollateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "Templates", "Type", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1001, 'Basic', 1, '.card {
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
INSERT INTO public."CollateInstance" ("Id", "Name", "CollateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "Templates", "Type", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1002, 'Basic (optional reversed card)', 2, '.card {
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
INSERT INTO public."CollateInstance" ("Id", "Name", "CollateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "Templates", "Type", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1003, 'Basic (and reversed card)', 3, '.card {
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
INSERT INTO public."CollateInstance" ("Id", "Name", "CollateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "Templates", "Type", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1004, 'Basic (type in the answer)', 4, '.card {
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
INSERT INTO public."CollateInstance" ("Id", "Name", "CollateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "Templates", "Type", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1005, 'Cloze', 5, '.card {
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
INSERT INTO public."CollateInstance" ("Id", "Name", "CollateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "Templates", "Type", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1006, 'Basic (type in the answer)', 4, '.card {
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
INSERT INTO public."CollateInstance" ("Id", "Name", "CollateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "Templates", "Type", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1007, 'Cloze', 5, '.card {
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




































INSERT INTO public."User" ("Id", "DisplayName", "DefaultCardSettingId", "ShowNextReviewTime", "ShowRemainingCardCount", "MixNewAndReview", "NextDayStartsAtXHoursPastMidnight", "LearnAheadLimitInMinutes", "TimeboxTimeLimitInMinutes", "IsNightMode") VALUES (1, 'Admin', 1, true, true, 0, 4, 20, 0, false);
INSERT INTO public."User" ("Id", "DisplayName", "DefaultCardSettingId", "ShowNextReviewTime", "ShowRemainingCardCount", "MixNewAndReview", "NextDayStartsAtXHoursPastMidnight", "LearnAheadLimitInMinutes", "TimeboxTimeLimitInMinutes", "IsNightMode") VALUES (2, 'The Collective', 2, true, true, 0, 4, 20, 0, false);
INSERT INTO public."User" ("Id", "DisplayName", "DefaultCardSettingId", "ShowNextReviewTime", "ShowRemainingCardCount", "MixNewAndReview", "NextDayStartsAtXHoursPastMidnight", "LearnAheadLimitInMinutes", "TimeboxTimeLimitInMinutes", "IsNightMode") VALUES (3, 'RoboTurtle', 3, true, true, 0, 4, 20, 0, false);


INSERT INTO public."User_CollateInstance" ("UserId", "CollateInstanceId", "DefaultCardSettingId") VALUES (3, 1001, 3);
INSERT INTO public."User_CollateInstance" ("UserId", "CollateInstanceId", "DefaultCardSettingId") VALUES (3, 1002, 3);
INSERT INTO public."User_CollateInstance" ("UserId", "CollateInstanceId", "DefaultCardSettingId") VALUES (3, 1003, 3);
INSERT INTO public."User_CollateInstance" ("UserId", "CollateInstanceId", "DefaultCardSettingId") VALUES (3, 1006, 3);
INSERT INTO public."User_CollateInstance" ("UserId", "CollateInstanceId", "DefaultCardSettingId") VALUES (3, 1005, 3);








SELECT pg_catalog.setval('public."AcquiredCard_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."AlphaBetaKey_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."BranchInstance_Id_seq"', 1001, false);


SELECT pg_catalog.setval('public."Branch_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CardSetting_Id_seq"', 4, false);


SELECT pg_catalog.setval('public."Card_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CollateInstance_Id_seq"', 1008, false);


SELECT pg_catalog.setval('public."Collate_Id_seq"', 6, false);


SELECT pg_catalog.setval('public."CommentCard_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CommentCollate_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CommunalFieldInstance_Id_seq"', 1001, false);


SELECT pg_catalog.setval('public."CommunalField_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Deck_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Feedback_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."File_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Filter_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."History_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."PotentialSignups_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Relationship_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Tag_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."User_Id_seq"', 4, false);


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "PK_AcquiredCard" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."AlphaBetaKey"
    ADD CONSTRAINT "PK_AlphaBetaKey" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Branch"
    ADD CONSTRAINT "PK_Branch" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "PK_BranchInstance" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "PK_Card" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CardSetting"
    ADD CONSTRAINT "PK_CardSetting" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Collate"
    ADD CONSTRAINT "PK_Collate" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CollateInstance"
    ADD CONSTRAINT "PK_CollateInstance" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "PK_CommentCard" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommentCollate"
    ADD CONSTRAINT "PK_CommentCollate" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommunalField"
    ADD CONSTRAINT "PK_CommunalField" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommunalFieldInstance"
    ADD CONSTRAINT "PK_CommunalFieldInstance" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommunalFieldInstance_BranchInstance"
    ADD CONSTRAINT "PK_CommunalFieldInstance_BranchInstance" PRIMARY KEY ("CommunalFieldInstanceId", "BranchInstanceId");


ALTER TABLE ONLY public."Deck"
    ADD CONSTRAINT "PK_Deck" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Feedback"
    ADD CONSTRAINT "PK_Feedback" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."File"
    ADD CONSTRAINT "PK_File" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."File_BranchInstance"
    ADD CONSTRAINT "PK_File_BranchInstance" PRIMARY KEY ("BranchInstanceId", "FileId");


ALTER TABLE ONLY public."Filter"
    ADD CONSTRAINT "PK_Filter" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."History"
    ADD CONSTRAINT "PK_History" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."PotentialSignups"
    ADD CONSTRAINT "PK_PotentialSignups" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Relationship"
    ADD CONSTRAINT "PK_Relationship" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "PK_Relationship_AcquiredCard" PRIMARY KEY ("RelationshipId", "UserId", "SourceCardId", "TargetCardId");


ALTER TABLE ONLY public."Tag"
    ADD CONSTRAINT "PK_Tag" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Tag_AcquiredCard"
    ADD CONSTRAINT "PK_Tag_AcquiredCard" PRIMARY KEY ("TagId", "UserId", "CardId");


ALTER TABLE ONLY public."Tag_User_CollateInstance"
    ADD CONSTRAINT "PK_Tag_User_CollateInstance" PRIMARY KEY ("UserId", "CollateInstanceId", "DefaultTagId");


ALTER TABLE ONLY public."User"
    ADD CONSTRAINT "PK_User" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."User_CollateInstance"
    ADD CONSTRAINT "PK_User_CollateInstance" PRIMARY KEY ("UserId", "CollateInstanceId");


ALTER TABLE ONLY public."Vote_CommentCard"
    ADD CONSTRAINT "PK_Vote_CommentCard" PRIMARY KEY ("CommentCardId", "UserId");


ALTER TABLE ONLY public."Vote_CommentCollate"
    ADD CONSTRAINT "PK_Vote_CommentCollate" PRIMARY KEY ("CommentCollateId", "UserId");


ALTER TABLE ONLY public."Vote_Feedback"
    ADD CONSTRAINT "PK_Vote_Feedback" PRIMARY KEY ("FeedbackId", "UserId");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "UQ_AcquiredCard_AcquiredCardId_UserId_CardId" UNIQUE ("Id", "UserId", "CardId");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "UQ_BranchInstance_BranchId_Id" UNIQUE ("BranchId", "Id");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "UQ_BranchInstance_CardId_Id" UNIQUE ("CardId", "Id");


ALTER TABLE ONLY public."Branch"
    ADD CONSTRAINT "UQ_Branch_CardId_Id" UNIQUE ("CardId", "Id");


CREATE INDEX "IX_AcquiredCard_BranchInstanceId" ON public."AcquiredCard" USING btree ("BranchInstanceId");


CREATE INDEX "IX_AcquiredCard_CardSettingId" ON public."AcquiredCard" USING btree ("CardSettingId");


CREATE INDEX "IX_AcquiredCard_CardState" ON public."AcquiredCard" USING btree ("CardState");


CREATE INDEX "IX_AcquiredCard_UserId" ON public."AcquiredCard" USING btree ("UserId");


CREATE INDEX "IX_AcquiredCard_UserId_BranchId" ON public."AcquiredCard" USING btree ("UserId", "BranchId");


CREATE UNIQUE INDEX "IX_AcquiredCard_UserId_BranchInstanceId_Index" ON public."AcquiredCard" USING btree ("UserId", "BranchInstanceId", "Index");


CREATE INDEX "IX_AcquiredCard_UserId_CardId" ON public."AcquiredCard" USING btree ("UserId", "CardId");


CREATE UNIQUE INDEX "IX_AlphaBetaKey_Key" ON public."AlphaBetaKey" USING btree ("Key");


CREATE INDEX "IX_BranchInstance_BranchId" ON public."BranchInstance" USING btree ("BranchId");


CREATE INDEX "IX_BranchInstance_CollateInstanceId" ON public."BranchInstance" USING btree ("CollateInstanceId");


CREATE INDEX "IX_BranchInstance_Hash" ON public."BranchInstance" USING btree ("Hash");


CREATE INDEX "IX_CardSetting_UserId" ON public."CardSetting" USING btree ("UserId");


CREATE INDEX "IX_Card_AuthorId" ON public."Card" USING btree ("AuthorId");


CREATE INDEX "IX_CollateInstance_CollateId" ON public."CollateInstance" USING btree ("CollateId");


CREATE INDEX "IX_CollateInstance_Hash" ON public."CollateInstance" USING btree ("Hash");


CREATE INDEX "IX_Collate_AuthorId" ON public."Collate" USING btree ("AuthorId");


CREATE INDEX "IX_CommentCard_CardId" ON public."CommentCard" USING btree ("CardId");


CREATE INDEX "IX_CommentCard_UserId" ON public."CommentCard" USING btree ("UserId");


CREATE INDEX "IX_CommentCollate_CollateId" ON public."CommentCollate" USING btree ("CollateId");


CREATE INDEX "IX_CommentCollate_UserId" ON public."CommentCollate" USING btree ("UserId");


CREATE INDEX "IX_CommunalFieldInstance_BranchInstance_BranchInstanceId" ON public."CommunalFieldInstance_BranchInstance" USING btree ("BranchInstanceId");


CREATE INDEX "IX_CommunalFieldInstance_CommunalFieldId" ON public."CommunalFieldInstance" USING btree ("CommunalFieldId");


CREATE INDEX "IX_CommunalField_AuthorId" ON public."CommunalField" USING btree ("AuthorId");


CREATE INDEX "IX_Feedback_ParentId" ON public."Feedback" USING btree ("ParentId");


CREATE INDEX "IX_Feedback_UserId" ON public."Feedback" USING btree ("UserId");


CREATE INDEX "IX_File_BranchInstance_FileId" ON public."File_BranchInstance" USING btree ("FileId");


CREATE UNIQUE INDEX "IX_File_Sha256" ON public."File" USING btree ("Sha256");


CREATE INDEX "IX_Filter_UserId" ON public."Filter" USING btree ("UserId");


CREATE INDEX "IX_History_AcquiredCardId" ON public."History" USING btree ("AcquiredCardId");


CREATE INDEX "IX_Relationship_AcquiredCard_RelationshipId" ON public."Relationship_AcquiredCard" USING btree ("RelationshipId");


CREATE INDEX "IX_Relationship_AcquiredCard_TargetAcquiredCardId" ON public."Relationship_AcquiredCard" USING btree ("TargetAcquiredCardId");


CREATE UNIQUE INDEX "IX_Relationship_Name" ON public."Relationship" USING btree (upper(("Name")::text));


CREATE INDEX "IX_Tag_AcquiredCard_AcquiredCardId" ON public."Tag_AcquiredCard" USING btree ("AcquiredCardId");


CREATE UNIQUE INDEX "IX_Tag_Name" ON public."Tag" USING btree (upper(("Name")::text));


CREATE INDEX "IX_Tag_User_CollateInstance_DefaultTagId" ON public."Tag_User_CollateInstance" USING btree ("DefaultTagId");


CREATE INDEX "IX_User_CollateInstance_CollateInstanceId" ON public."User_CollateInstance" USING btree ("CollateInstanceId");


CREATE INDEX "IX_User_CollateInstance_DefaultCardSettingId" ON public."User_CollateInstance" USING btree ("DefaultCardSettingId");


CREATE INDEX "IX_Vote_CommentCard_UserId" ON public."Vote_CommentCard" USING btree ("UserId");


CREATE INDEX "IX_Vote_CommentCollate_UserId" ON public."Vote_CommentCollate" USING btree ("UserId");


CREATE INDEX "IX_Vote_Feedback_UserId" ON public."Vote_Feedback" USING btree ("UserId");


CREATE UNIQUE INDEX "UQ_Branch_CardId_Name" ON public."Branch" USING btree ("CardId", upper(("Name")::text));


CREATE UNIQUE INDEX "UQ_Deck_UserId_Name" ON public."Deck" USING btree ("UserId", upper(("Name")::text));


CREATE INDEX idx_fts_branchinstance_tsvector ON public."BranchInstance" USING gin ("TsVector");


CREATE INDEX idx_fts_collateinstance_tsvector ON public."CollateInstance" USING gin ("TsVector");


CREATE INDEX idx_fts_communalfieldinstance_tsvector ON public."CommunalFieldInstance" USING gin ("TsVector");


CREATE INDEX idx_fts_relationship_tsvector ON public."Relationship" USING gin ("TsVector");


CREATE INDEX idx_fts_tag_tsvector ON public."Tag" USING gin ("TsVector");


CREATE TRIGGER tr_acquiredcard_afterinsertdeleteupdate AFTER INSERT OR DELETE OR UPDATE ON public."AcquiredCard" FOR EACH ROW EXECUTE FUNCTION public.fn_acquiredcard_afterinsertdeleteupdate();


CREATE TRIGGER tr_acquiredcard_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."AcquiredCard" FOR EACH ROW EXECUTE FUNCTION public.fn_acquiredcard_beforeinsertupdate();


CREATE TRIGGER tr_branch_afterinsertupdate AFTER INSERT OR UPDATE ON public."Branch" FOR EACH ROW EXECUTE FUNCTION public.fn_branch_afterinsertupdate();


CREATE TRIGGER tr_branchinstance_beforeinsert BEFORE INSERT ON public."BranchInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_branchinstance_beforeinsert();


CREATE TRIGGER tr_collateinstance_beforeinsert BEFORE INSERT ON public."CollateInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_collateinstance_beforeinsert();


CREATE TRIGGER tr_communalfieldinstance_beforeinsert BEFORE INSERT ON public."CommunalFieldInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_communalfieldinstance_beforeinsert();


CREATE TRIGGER tr_deck_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."Deck" FOR EACH ROW EXECUTE FUNCTION public.fn_deck_beforeinsertupdate();


CREATE TRIGGER tr_relationship_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."Relationship" FOR EACH ROW EXECUTE FUNCTION public.fn_relationship_beforeinsertupdate();


CREATE TRIGGER tr_tag_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."Tag" FOR EACH ROW EXECUTE FUNCTION public.fn_tag_beforeinsertupdate();


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_BranchInstance_BranchInstanceId" FOREIGN KEY ("BranchInstanceId") REFERENCES public."BranchInstance"("Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_BranchInstance_BranchInstanceId_BranchId" FOREIGN KEY ("BranchId", "BranchInstanceId") REFERENCES public."BranchInstance"("BranchId", "Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_Branch_BranchId" FOREIGN KEY ("BranchId") REFERENCES public."Branch"("Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_Branch_BranchId_CardId" FOREIGN KEY ("CardId", "BranchId") REFERENCES public."Branch"("CardId", "Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_CardSetting_CardSettingId" FOREIGN KEY ("CardSettingId") REFERENCES public."CardSetting"("Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_Card_CardId" FOREIGN KEY ("CardId") REFERENCES public."Card"("Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_Deck_DeckId" FOREIGN KEY ("DeckId") REFERENCES public."Deck"("Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "FK_BranchInstance_Branch_BranchId" FOREIGN KEY ("BranchId") REFERENCES public."Branch"("Id");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "FK_BranchInstance_Branch_CardId_BranchId" FOREIGN KEY ("CardId", "BranchId") REFERENCES public."Branch"("CardId", "Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "FK_BranchInstance_CollateInstance_CollateInstanceId" FOREIGN KEY ("CollateInstanceId") REFERENCES public."CollateInstance"("Id");


ALTER TABLE ONLY public."Branch"
    ADD CONSTRAINT "FK_Branch_BranchInstance_LatestInstanceId" FOREIGN KEY ("LatestInstanceId") REFERENCES public."BranchInstance"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."Branch"
    ADD CONSTRAINT "FK_Branch_Card_CardId" FOREIGN KEY ("CardId") REFERENCES public."Card"("Id");


ALTER TABLE ONLY public."Branch"
    ADD CONSTRAINT "FK_Branch_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CardSetting"
    ADD CONSTRAINT "FK_CardSetting_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "FK_Card_BranchInstance_CopySourceId" FOREIGN KEY ("CopySourceId") REFERENCES public."BranchInstance"("Id");


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "FK_Card_Branch_DefaultBranchId" FOREIGN KEY ("DefaultBranchId") REFERENCES public."Branch"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "FK_Card_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CollateInstance"
    ADD CONSTRAINT "FK_CollateInstance_Collate_CollateId" FOREIGN KEY ("CollateId") REFERENCES public."Collate"("Id");


ALTER TABLE ONLY public."Collate"
    ADD CONSTRAINT "FK_Collate_CollateInstance_LatestInstanceId" FOREIGN KEY ("LatestInstanceId") REFERENCES public."CollateInstance"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."Collate"
    ADD CONSTRAINT "FK_Collate_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance_BranchInstance"
    ADD CONSTRAINT "FK_CommFieldInst_CardInst_CommFieldInst_CommFieldInstId" FOREIGN KEY ("CommunalFieldInstanceId") REFERENCES public."CommunalFieldInstance"("Id");


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "FK_CommentCard_Card_CardId" FOREIGN KEY ("CardId") REFERENCES public."Card"("Id");


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "FK_CommentCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommentCollate"
    ADD CONSTRAINT "FK_CommentCollate_Collate_CollateId" FOREIGN KEY ("CollateId") REFERENCES public."Collate"("Id");


ALTER TABLE ONLY public."CommentCollate"
    ADD CONSTRAINT "FK_CommentCollate_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance_BranchInstance"
    ADD CONSTRAINT "FK_CommunalFieldInst_CardInst_CardInst_CardInstId" FOREIGN KEY ("BranchInstanceId") REFERENCES public."BranchInstance"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance"
    ADD CONSTRAINT "FK_CommunalFieldInstance_CommunalField_CommunalFieldId" FOREIGN KEY ("CommunalFieldId") REFERENCES public."CommunalField"("Id");


ALTER TABLE ONLY public."CommunalField"
    ADD CONSTRAINT "FK_CommunalField_CommunalFieldInstance_LatestInstanceId" FOREIGN KEY ("LatestInstanceId") REFERENCES public."CommunalFieldInstance"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."CommunalField"
    ADD CONSTRAINT "FK_CommunalField_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Deck"
    ADD CONSTRAINT "FK_Deck_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Feedback"
    ADD CONSTRAINT "FK_Feedback_Feedback_ParentId" FOREIGN KEY ("ParentId") REFERENCES public."Feedback"("Id");


ALTER TABLE ONLY public."Feedback"
    ADD CONSTRAINT "FK_Feedback_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."File_BranchInstance"
    ADD CONSTRAINT "FK_File_BranchInstance_BranchInstance_BranchInstanceId" FOREIGN KEY ("BranchInstanceId") REFERENCES public."BranchInstance"("Id");


ALTER TABLE ONLY public."File_BranchInstance"
    ADD CONSTRAINT "FK_File_BranchInstance_File_FileId" FOREIGN KEY ("FileId") REFERENCES public."File"("Id");


ALTER TABLE ONLY public."Filter"
    ADD CONSTRAINT "FK_Filter_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."History"
    ADD CONSTRAINT "FK_History_AcquiredCard_AcquiredCardId" FOREIGN KEY ("AcquiredCardId") REFERENCES public."AcquiredCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "FK_Relationship_AcquiredCard_AcquiredCard_SourceAcquiredCardId" FOREIGN KEY ("SourceAcquiredCardId") REFERENCES public."AcquiredCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "FK_Relationship_AcquiredCard_AcquiredCard_TargetAcquiredCardId" FOREIGN KEY ("TargetAcquiredCardId") REFERENCES public."AcquiredCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "FK_Relationship_AcquiredCard_Relationship_RelationshipId" FOREIGN KEY ("RelationshipId") REFERENCES public."Relationship"("Id");


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "FK_Relationship_AcquiredCard_SourceAcquiredCard_UserId_CardId" FOREIGN KEY ("SourceAcquiredCardId", "UserId", "SourceCardId") REFERENCES public."AcquiredCard"("Id", "UserId", "CardId");


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "FK_Relationship_AcquiredCard_TargetAcquiredCard_UserId_CardId" FOREIGN KEY ("TargetAcquiredCardId", "UserId", "TargetCardId") REFERENCES public."AcquiredCard"("Id", "UserId", "CardId");


ALTER TABLE ONLY public."Tag_AcquiredCard"
    ADD CONSTRAINT "FK_Tag_AcquiredCard_AcquiredCardId_UserId_CardId" FOREIGN KEY ("AcquiredCardId", "UserId", "CardId") REFERENCES public."AcquiredCard"("Id", "UserId", "CardId") ON DELETE CASCADE;


ALTER TABLE ONLY public."Tag_AcquiredCard"
    ADD CONSTRAINT "FK_Tag_AcquiredCard_AcquiredCard_AcquiredCardId" FOREIGN KEY ("AcquiredCardId") REFERENCES public."AcquiredCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."Tag_AcquiredCard"
    ADD CONSTRAINT "FK_Tag_AcquiredCard_Tag_TagId" FOREIGN KEY ("TagId") REFERENCES public."Tag"("Id");


ALTER TABLE ONLY public."Tag_User_CollateInstance"
    ADD CONSTRAINT "FK_Tag_User_CollateInstance_Tag_DefaultTagId" FOREIGN KEY ("DefaultTagId") REFERENCES public."Tag"("Id");


ALTER TABLE ONLY public."Tag_User_CollateInstance"
    ADD CONSTRAINT "FK_Tag_User_TemplatInst_User_TemplatInst_UserId_TemplatInstId" FOREIGN KEY ("UserId", "CollateInstanceId") REFERENCES public."User_CollateInstance"("UserId", "CollateInstanceId");


ALTER TABLE ONLY public."User"
    ADD CONSTRAINT "FK_User_CardSetting_DefaultCardSettingId" FOREIGN KEY ("DefaultCardSettingId") REFERENCES public."CardSetting"("Id");


ALTER TABLE ONLY public."User_CollateInstance"
    ADD CONSTRAINT "FK_User_CollateInstance_CardSetting_DefaultCardSettingId" FOREIGN KEY ("DefaultCardSettingId") REFERENCES public."CardSetting"("Id");


ALTER TABLE ONLY public."User_CollateInstance"
    ADD CONSTRAINT "FK_User_CollateInstance_CollateInstance_CollateInstanceId" FOREIGN KEY ("CollateInstanceId") REFERENCES public."CollateInstance"("Id");


ALTER TABLE ONLY public."User_CollateInstance"
    ADD CONSTRAINT "FK_User_CollateInstance_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Vote_CommentCard"
    ADD CONSTRAINT "FK_Vote_CommentCard_CommentCard_CommentCardId" FOREIGN KEY ("CommentCardId") REFERENCES public."CommentCard"("Id");


ALTER TABLE ONLY public."Vote_CommentCard"
    ADD CONSTRAINT "FK_Vote_CommentCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Vote_CommentCollate"
    ADD CONSTRAINT "FK_Vote_CommentCollate_CommentCollate_CommentCollateId" FOREIGN KEY ("CommentCollateId") REFERENCES public."CommentCollate"("Id");


ALTER TABLE ONLY public."Vote_CommentCollate"
    ADD CONSTRAINT "FK_Vote_CommentCollate_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Vote_Feedback"
    ADD CONSTRAINT "FK_Vote_Feedback_Feedback_FeedbackId" FOREIGN KEY ("FeedbackId") REFERENCES public."Feedback"("Id");


ALTER TABLE ONLY public."Vote_Feedback"
    ADD CONSTRAINT "FK_Vote_Feedback_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");



