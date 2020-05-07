-- lowTODO: make a trigger to ensure that [dbo].[Relationship_AcquiredCard]'s AcquiredCard's UserIds are the same. Do *not* use a CHECK CONSTRAINT; those are unreliable
-- "Latest*" Sql Views come from https://stackoverflow.com/a/2111420

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
		IF (TG_OP = 'DELETE' OR (TG_OP = 'UPDATE' AND (OLD."BranchInstanceId" <> NEW."BranchInstanceId" OR OLD."CardState" <> NEW."CardState"))) THEN
            UPDATE	"BranchInstance" ci
            SET		"Users" = ( SELECT Count(*)
                                FROM "AcquiredCard"
                                WHERE "BranchInstanceId" = OLD."BranchInstanceId" AND "CardState" <> 3 )
            WHERE	ci."Id" = OLD."BranchInstanceId";
            UPDATE  "Card" branchSource -- https://stackoverflow.com/a/34806364
            SET     "Users" = ( SELECT  COUNT(*)
                                FROM    "Card" c
                                JOIN    "AcquiredCard" ac on ac."CardId" = c."Id"
                                WHERE   ac."CardState" <> 3 )
            FROM    "Card" c1
            LEFT JOIN "Card" c2 ON c1."Id" = c2."Id" AND c2."Id" = OLD."CardId"
            WHERE branchSource."Id" = c1."Id";
        END IF;
        IF (TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND (OLD."BranchInstanceId" <> NEW."BranchInstanceId" OR OLD."CardState" <> NEW."CardState"))) THEN
            UPDATE	"BranchInstance" ci
            SET		"Users" = ( SELECT Count(*)
                                FROM "AcquiredCard"
                                WHERE "BranchInstanceId" = NEW."BranchInstanceId" AND "CardState" <> 3 )
            WHERE	ci."Id" = NEW."BranchInstanceId";
            UPDATE  "Card" branchSource -- https://stackoverflow.com/a/34806364
            SET     "Users" = ( SELECT  COUNT(*)
                                FROM    "Card" c
                                JOIN    "AcquiredCard" ac on ac."CardId" = c."Id"
                                WHERE   ac."CardState" <> 3 )
            FROM    "Card" c1
            LEFT JOIN "Card" c2 ON c1."Id" = c2."Id" AND c2."Id" = NEW."CardId"
            WHERE branchSource."Id" = c1."Id";
        END IF;
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_acquiredcard_afterinsertdeleteupdate() OWNER TO postgres;

CREATE FUNCTION public.fn_branch_afterinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        UPDATE "Card" c
        SET    "DefaultBranchId" = (NEW."Id")
        WHERE (c."Id" = NEW."CardId" AND c."DefaultBranchId" = 0);
        RETURN NULL;
    END;
$$;


ALTER FUNCTION public.fn_branch_afterinsert() OWNER TO postgres;

CREATE FUNCTION public.fn_acquiredcard_beforeinsertupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
    BEGIN
        IF (NEW."TsVectorHelper" IS NOT NULL) THEN
            NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."TsVectorHelper");
            NEW."TsVectorHelper" = NULL;
        END IF;
        RETURN NEW;
    END;
$$;


ALTER FUNCTION public.fn_acquiredcard_beforeinsertupdate() OWNER TO postgres;

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

CREATE FUNCTION public.fn_templateinstance_beforeinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  UPDATE "Template" t
  SET    "LatestInstanceId" = NEW."Id"
  WHERE  t."Id" = NEW."TemplateId";
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


ALTER FUNCTION public.fn_templateinstance_beforeinsert() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE TABLE public."AcquiredCard" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "CardId" integer NOT NULL,
    "BranchId" integer NOT NULL,
    "BranchInstanceId" integer NOT NULL,
    "CardState" smallint NOT NULL,
    "EaseFactorInPermille" smallint NOT NULL,
    "IntervalOrStepsIndex" smallint NOT NULL,
    "Due" timestamp without time zone NOT NULL,
    "CardSettingId" integer NOT NULL,
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
    "TemplateInstanceId" integer NOT NULL,
    "Users" integer NOT NULL,
    "EditSummary" character varying(200) NOT NULL,
    "AnkiNoteId" bigint,
    "AnkiNoteOrd" smallint,
    "Hash" bit(512) NOT NULL,
    "TsVectorHelper" text,
    "TsVector" tsvector,
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
    "SourceAcquiredCardId" integer NOT NULL,
    "TargetAcquiredCardId" integer NOT NULL,
    "RelationshipId" integer NOT NULL
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


CREATE TABLE public."CommentTemplate" (
    "Id" integer NOT NULL,
    "TemplateId" integer NOT NULL,
    "UserId" integer NOT NULL,
    "Text" character varying(500) NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "IsDmca" boolean NOT NULL
);


ALTER TABLE public."CommentTemplate" OWNER TO postgres;

ALTER TABLE public."CommentTemplate" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CommentTemplate_Id_seq"
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


CREATE TABLE public."Tag_User_TemplateInstance" (
    "UserId" integer NOT NULL,
    "TemplateInstanceId" integer NOT NULL,
    "DefaultTagId" integer NOT NULL
);


ALTER TABLE public."Tag_User_TemplateInstance" OWNER TO postgres;

CREATE TABLE public."Template" (
    "Id" integer NOT NULL,
    "AuthorId" integer NOT NULL,
    "LatestInstanceId" integer NOT NULL,
    "IsListed" boolean NOT NULL
);


ALTER TABLE public."Template" OWNER TO postgres;

CREATE TABLE public."TemplateInstance" (
    "Id" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "TemplateId" integer NOT NULL,
    "Css" character varying(4000) NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "Modified" timestamp without time zone,
    "LatexPre" character varying(500) NOT NULL,
    "LatexPost" character varying(500) NOT NULL,
    "IsDmca" boolean NOT NULL,
    "QuestionTemplate" character varying(4000) NOT NULL,
    "AnswerTemplate" character varying(4000) NOT NULL,
    "ShortQuestionTemplate" character varying(200) NOT NULL,
    "ShortAnswerTemplate" character varying(200) NOT NULL,
    "Fields" character varying(4000) NOT NULL,
    "EditSummary" character varying(200) NOT NULL,
    "AnkiId" bigint,
    "Hash" bit(512) NOT NULL,
    "CWeightTsVectorHelper" text,
    "TsVector" tsvector,
    CONSTRAINT "TemplateInstance_CWeightTsVectorHelper_IsNull" CHECK (("CWeightTsVectorHelper" IS NULL))
);


ALTER TABLE public."TemplateInstance" OWNER TO postgres;

ALTER TABLE public."TemplateInstance" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."TemplateInstance_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public."Template" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Template_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


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

ALTER TABLE public."User" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."User_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE TABLE public."User_TemplateInstance" (
    "UserId" integer NOT NULL,
    "TemplateInstanceId" integer NOT NULL,
    "DefaultCardSettingId" integer NOT NULL
);


ALTER TABLE public."User_TemplateInstance" OWNER TO postgres;

CREATE TABLE public."Vote_CommentCard" (
    "CommentCardId" integer NOT NULL,
    "UserId" integer NOT NULL
);


ALTER TABLE public."Vote_CommentCard" OWNER TO postgres;

CREATE TABLE public."Vote_CommentTemplate" (
    "CommentTemplateId" integer NOT NULL,
    "UserId" integer NOT NULL
);


ALTER TABLE public."Vote_CommentTemplate" OWNER TO postgres;

CREATE TABLE public."Vote_Feedback" (
    "FeedbackId" integer NOT NULL,
    "UserId" integer NOT NULL
);


ALTER TABLE public."Vote_Feedback" OWNER TO postgres;











INSERT INTO public."CardSetting" ("Id", "UserId", "Name", "NewCardsStepsInMinutes", "NewCardsMaxPerDay", "NewCardsGraduatingIntervalInDays", "NewCardsEasyIntervalInDays", "NewCardsStartingEaseFactorInPermille", "NewCardsBuryRelated", "MatureCardsMaxPerDay", "MatureCardsEaseFactorEasyBonusFactorInPermille", "MatureCardsIntervalFactorInPermille", "MatureCardsMaximumIntervalInDays", "MatureCardsHardIntervalFactorInPermille", "MatureCardsBuryRelated", "LapsedCardsStepsInMinutes", "LapsedCardsNewIntervalFactorInPermille", "LapsedCardsMinimumIntervalInDays", "LapsedCardsLeechThreshold", "ShowAnswerTimer", "AutomaticallyPlayAudio", "ReplayQuestionAudioOnAnswer") VALUES (1, 1, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public."CardSetting" ("Id", "UserId", "Name", "NewCardsStepsInMinutes", "NewCardsMaxPerDay", "NewCardsGraduatingIntervalInDays", "NewCardsEasyIntervalInDays", "NewCardsStartingEaseFactorInPermille", "NewCardsBuryRelated", "MatureCardsMaxPerDay", "MatureCardsEaseFactorEasyBonusFactorInPermille", "MatureCardsIntervalFactorInPermille", "MatureCardsMaximumIntervalInDays", "MatureCardsHardIntervalFactorInPermille", "MatureCardsBuryRelated", "LapsedCardsStepsInMinutes", "LapsedCardsNewIntervalFactorInPermille", "LapsedCardsMinimumIntervalInDays", "LapsedCardsLeechThreshold", "ShowAnswerTimer", "AutomaticallyPlayAudio", "ReplayQuestionAudioOnAnswer") VALUES (2, 2, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);
INSERT INTO public."CardSetting" ("Id", "UserId", "Name", "NewCardsStepsInMinutes", "NewCardsMaxPerDay", "NewCardsGraduatingIntervalInDays", "NewCardsEasyIntervalInDays", "NewCardsStartingEaseFactorInPermille", "NewCardsBuryRelated", "MatureCardsMaxPerDay", "MatureCardsEaseFactorEasyBonusFactorInPermille", "MatureCardsIntervalFactorInPermille", "MatureCardsMaximumIntervalInDays", "MatureCardsHardIntervalFactorInPermille", "MatureCardsBuryRelated", "LapsedCardsStepsInMinutes", "LapsedCardsNewIntervalFactorInPermille", "LapsedCardsMinimumIntervalInDays", "LapsedCardsLeechThreshold", "ShowAnswerTimer", "AutomaticallyPlayAudio", "ReplayQuestionAudioOnAnswer") VALUES (3, 3, 'Default', '1 10', 20, 1, 4, 2500, true, 200, 1300, 1000, 32767, 1200, true, '10', 0, 1, 8, false, false, false);


































INSERT INTO public."Template" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (1, 2, 1, true);
INSERT INTO public."Template" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (2, 2, 2, true);
INSERT INTO public."Template" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (3, 2, 3, true);
INSERT INTO public."Template" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (4, 2, 6, true);
INSERT INTO public."Template" ("Id", "AuthorId", "LatestInstanceId", "IsListed") VALUES (5, 2, 7, true);


INSERT INTO public."TemplateInstance" ("Id", "Name", "TemplateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "QuestionTemplate", "AnswerTemplate", "ShortQuestionTemplate", "ShortAnswerTemplate", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (1, 'Basic', 1, '.card {
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
', '\end{document}', false, '{{Front}}', '{{FrontSide}}

<hr id=answer>

{{Back}}', '', '', 'FrontFalse0FalseBackFalse1False', 'Imported from Anki', 1554689669581, B'11111100100010011000001111010100110110100110111111111100101101001011011111010111011001100001011111001000110101010000100010110001010010010000001001000110010101011011111000010010100100001000000100001011101100111011101011000001100111111011000111111101010110100111001011000101011110101101010011100101000111011001111100011001010000001011001010110010110011001000010010110000000010001110011101010111011111111011001110101110010010000110111111011110000111000011111110101111000001000111111010010010110011100111101001011111', NULL, '''20px'':15 ''align'':18 ''arial'':11 ''back'':3C,6C ''background'':23 ''background-color'':22 ''basic'':1A ''black'':21 ''card'':7 ''center'':19 ''color'':20,24 ''famili'':10 ''font'':9,13 ''font-famili'':8 ''font-siz'':12 ''front'':2C,4C ''frontsid'':5C ''size'':14 ''text'':17 ''text-align'':16 ''white'':25');
INSERT INTO public."TemplateInstance" ("Id", "Name", "TemplateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "QuestionTemplate", "AnswerTemplate", "ShortQuestionTemplate", "ShortAnswerTemplate", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (2, 'Basic (and reversed card) - Card 1', 2, '.card {
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
', '\end{document}', false, '{{Front}}', '{{FrontSide}}

<hr id=answer>

{{Back}}', '', '', 'FrontFalse0FalseBackFalse1False', 'Imported from Anki', 1554689669577, B'01011110100111011111011110001111011100100011111000111010101010110010011101001110100111011001110000110111111010001101000010110001011001011010101111000101001110001011011001001001110100111100100000101100000011111111101110111100011001001101101111000001000110001110001111101011110100001110110000111010110001110011010100001010110101000110011111100101110000111011001000010100101011111101110000001011000100101000001001000101000110101101001000010111011011001110000000111000010111010100111010010111010111101111010100101110', NULL, '''1'':6A ''20px'':20 ''align'':23 ''arial'':16 ''back'':8C,11C ''background'':28 ''background-color'':27 ''basic'':1A ''black'':26 ''card'':4A,5A,12 ''center'':24 ''color'':25,29 ''famili'':15 ''font'':14,18 ''font-famili'':13 ''font-siz'':17 ''front'':7C,9C ''frontsid'':10C ''revers'':3A ''size'':19 ''text'':22 ''text-align'':21 ''white'':30');
INSERT INTO public."TemplateInstance" ("Id", "Name", "TemplateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "QuestionTemplate", "AnswerTemplate", "ShortQuestionTemplate", "ShortAnswerTemplate", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (3, 'Basic (optional reversed card) - Card 1', 3, '.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', '2019-04-08 02:14:29.572', '2019-06-16 00:51:32', '\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', '\end{document}', false, '{{Front}}', '{{FrontSide}}

<hr id=answer>

{{Back}}', '', '', 'FrontFalse0FalseBackFalse1FalseAdd ReverseFalse2False', 'Imported from Anki', 1554689669572, B'00111110001110111101000000011000000001011000010111110000101011101000101010100110000000111001111001011000010101001100001011111000101000111010100111001010100000110000010100110110101000100011101000111110101101110000000111010001001001111011000011110011011101001100010110100100000101000100111110100110100010011100101010100100100101111111011011110001000010010000010011001001100011111100011101011010011110001000110010010001110111110101011111100110001100011011110101111110010000100111101011011011100101011011100110111011', NULL, '''1'':6A ''20px'':22 ''add'':9C ''align'':25 ''arial'':18 ''back'':8C,13C ''background'':30 ''background-color'':29 ''basic'':1A ''black'':28 ''card'':4A,5A,14 ''center'':26 ''color'':27,31 ''famili'':17 ''font'':16,20 ''font-famili'':15 ''font-siz'':19 ''front'':7C,11C ''frontsid'':12C ''option'':2A ''revers'':3A,10C ''size'':21 ''text'':24 ''text-align'':23 ''white'':32');
INSERT INTO public."TemplateInstance" ("Id", "Name", "TemplateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "QuestionTemplate", "AnswerTemplate", "ShortQuestionTemplate", "ShortAnswerTemplate", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (4, 'Basic (type in the answer)', 4, '.card {
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
', '\end{document}', false, '{{Front}}
{{type:Back}}', '{{FrontSide}}

<hr id=answer>

{{Back}}', '', '', 'FrontFalse0FalseBackFalse1False', 'Imported from Anki', 1587663611565, B'11110111000001111010010111101000100011111111010111101000010000101011111110111100011010100111110110111010101000110100111101110111011100000101111011011001110000110011100101001010010011101100101101110101011001000100111101011001111010111000110101110010110111001000110001111100101000010000001111011110110100100010010110000100100101111101100100011000010000000111110101000111001000111010110110011001001101100010001111100010101010111010111110100000101010111000111010011111000110111011101001011000100111101101010000001101', NULL, '''20px'':21 ''align'':24 ''answer'':5A ''arial'':17 ''back'':7C,10C,12C ''background'':29 ''background-color'':28 ''basic'':1A ''black'':27 ''card'':13 ''center'':25 ''color'':26,30 ''famili'':16 ''font'':15,19 ''font-famili'':14 ''font-siz'':18 ''front'':6C,8C ''frontsid'':11C ''size'':20 ''text'':23 ''text-align'':22 ''type'':2A,9C ''white'':31');
INSERT INTO public."TemplateInstance" ("Id", "Name", "TemplateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "QuestionTemplate", "AnswerTemplate", "ShortQuestionTemplate", "ShortAnswerTemplate", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (5, 'Cloze', 5, '.card {
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
', '\end{document}', false, '{{cloze:Text}}', '{{cloze:Text}}<br>
{{Extra}}', '', '', 'TextFalse0FalseExtraFalse1False', 'Imported from Anki', 1554689669570, B'11110010001110100101000001101001011010001100001010011010110010000001110011110110111101101011100010101111010000001000000000111101010001110001001111011011010101100011010100010111000000010100111011111100000111011000101001010000101111110101111001001001010101001011000100100001000100010001110100011000110110000111101101010011000100010110001110011001111000111010001011000101001011111001100011101101001110000101110000110001000100000001011100010110000000100110111110110110111101010111101100010011111001011111110101110011', NULL, '''20px'':17 ''align'':20 ''arial'':13 ''background'':25 ''background-color'':24 ''black'':23 ''blue'':34 ''bold'':32 ''card'':9 ''center'':21 ''cloze'':1A,4C,6C,28,36 ''color'':22,26,33,37 ''extra'':3C,8C ''famili'':12 ''font'':11,15,30 ''font-famili'':10 ''font-siz'':14 ''font-weight'':29 ''lightblu'':38 ''nightmod'':35 ''size'':16 ''text'':2C,5C,7C,19 ''text-align'':18 ''weight'':31 ''white'':27');
INSERT INTO public."TemplateInstance" ("Id", "Name", "TemplateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "QuestionTemplate", "AnswerTemplate", "ShortQuestionTemplate", "ShortAnswerTemplate", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (6, 'Basic (type in the answer)', 4, '.card {
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
', '\end{document}', false, '{{Front}}

{{type:Back}}', '{{Front}}

<hr id=answer>

{{type:Back}}', '', '', 'FrontFalse0FalseBackFalse1False', 'Answer uses {{Front}} instead of {{FrontSide}} and {{type:Back}} instead of {{Back}} as of Anki v2.1.15', 1587486094455, B'11010011100110101101000101000011011011110100001001110001001100101100101000010001110111010110111110011010001110101000101001001110011000010001010011011101100001011011111001101010100000101111110010110110111110011110010111100011111111100001111000101000101101000111111010101111001010111101011110000011000001000101110110110101000101100000000101100011110001100110110100100000101000000101100110110101101101101101110010111101110000100111010011100111001010000101110110101010010011110100111000011000001011100101111100110111', NULL, '''20px'':22 ''align'':25 ''answer'':5A ''arial'':18 ''back'':7C,10C,13C ''background'':30 ''background-color'':29 ''basic'':1A ''black'':28 ''card'':14 ''center'':26 ''color'':27,31 ''famili'':17 ''font'':16,20 ''font-famili'':15 ''font-siz'':19 ''front'':6C,8C,11C ''size'':21 ''text'':24 ''text-align'':23 ''type'':2A,9C,12C ''white'':32');
INSERT INTO public."TemplateInstance" ("Id", "Name", "TemplateId", "Css", "Created", "Modified", "LatexPre", "LatexPost", "IsDmca", "QuestionTemplate", "AnswerTemplate", "ShortQuestionTemplate", "ShortAnswerTemplate", "Fields", "EditSummary", "AnkiId", "Hash", "CWeightTsVectorHelper", "TsVector") VALUES (7, 'Cloze', 5, '.card {
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
', '\end{document}', false, '{{cloze:Text}}', '{{cloze:Text}}', '', '', 'TextFalse0False', '"Extra" field removed in Anki v2.1.22', 1587670846820, B'10010000111110000001010110111011100010111011010011111111001101001001001000011101101100010111100010111110001011000100111001011100001101101001100110100001101100000011101101110011001011111111100101000000101111100101001011001001001001000001101111110100001101100001111110000000011100011100111010011101011110111111100010100100010000001100001110000101001011010101110000100001111111010101001000110001100100101000110001101100011110111000010111011010101111011011011000111011011001100111000111000000010000010101011111011111', NULL, '''20px'':15 ''align'':18 ''arial'':11 ''background'':23 ''background-color'':22 ''black'':21 ''blue'':32 ''bold'':30 ''card'':7 ''center'':19 ''cloze'':1A,3C,5C,26,34 ''color'':20,24,31,35 ''famili'':10 ''font'':9,13,28 ''font-famili'':8 ''font-siz'':12 ''font-weight'':27 ''lightblu'':36 ''nightmod'':33 ''size'':14 ''text'':2C,4C,6C,17 ''text-align'':16 ''weight'':29 ''white'':25');


INSERT INTO public."User" ("Id", "DisplayName", "DefaultCardSettingId", "ShowNextReviewTime", "ShowRemainingCardCount", "MixNewAndReview", "NextDayStartsAtXHoursPastMidnight", "LearnAheadLimitInMinutes", "TimeboxTimeLimitInMinutes", "IsNightMode") VALUES (1, 'Admin', 1, true, true, 0, 4, 20, 0, false);
INSERT INTO public."User" ("Id", "DisplayName", "DefaultCardSettingId", "ShowNextReviewTime", "ShowRemainingCardCount", "MixNewAndReview", "NextDayStartsAtXHoursPastMidnight", "LearnAheadLimitInMinutes", "TimeboxTimeLimitInMinutes", "IsNightMode") VALUES (2, 'The Collective', 2, true, true, 0, 4, 20, 0, false);
INSERT INTO public."User" ("Id", "DisplayName", "DefaultCardSettingId", "ShowNextReviewTime", "ShowRemainingCardCount", "MixNewAndReview", "NextDayStartsAtXHoursPastMidnight", "LearnAheadLimitInMinutes", "TimeboxTimeLimitInMinutes", "IsNightMode") VALUES (3, 'RoboTurtle', 3, true, true, 0, 4, 20, 0, false);


INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (1, 1, 1);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (1, 2, 1);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (1, 3, 1);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (1, 6, 1);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (1, 5, 1);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (2, 1, 2);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (2, 2, 2);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (2, 3, 2);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (2, 6, 2);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (2, 5, 2);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (3, 1, 3);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (3, 2, 3);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (3, 3, 3);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (3, 6, 3);
INSERT INTO public."User_TemplateInstance" ("UserId", "TemplateInstanceId", "DefaultCardSettingId") VALUES (3, 5, 3);








SELECT pg_catalog.setval('public."AcquiredCard_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."AlphaBetaKey_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."BranchInstance_Id_seq"', 1001, false);


SELECT pg_catalog.setval('public."Branch_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CardSetting_Id_seq"', 4, false);


SELECT pg_catalog.setval('public."Card_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CommentCard_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CommentTemplate_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."CommunalFieldInstance_Id_seq"', 1001, false);


SELECT pg_catalog.setval('public."CommunalField_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Feedback_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."File_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Filter_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."History_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."PotentialSignups_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Relationship_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."Tag_Id_seq"', 1, false);


SELECT pg_catalog.setval('public."TemplateInstance_Id_seq"', 1001, false);


SELECT pg_catalog.setval('public."Template_Id_seq"', 6, false);


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


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "PK_CommentCard" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommentTemplate"
    ADD CONSTRAINT "PK_CommentTemplate" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommunalField"
    ADD CONSTRAINT "PK_CommunalField" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommunalFieldInstance"
    ADD CONSTRAINT "PK_CommunalFieldInstance" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CommunalFieldInstance_BranchInstance"
    ADD CONSTRAINT "PK_CommunalFieldInstance_BranchInstance" PRIMARY KEY ("CommunalFieldInstanceId", "BranchInstanceId");


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
    ADD CONSTRAINT "PK_Relationship_AcquiredCard" PRIMARY KEY ("SourceAcquiredCardId", "TargetAcquiredCardId", "RelationshipId");


ALTER TABLE ONLY public."Tag"
    ADD CONSTRAINT "PK_Tag" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."Tag_AcquiredCard"
    ADD CONSTRAINT "PK_Tag_AcquiredCard" PRIMARY KEY ("TagId", "AcquiredCardId");


ALTER TABLE ONLY public."Tag_User_TemplateInstance"
    ADD CONSTRAINT "PK_Tag_User_TemplateInstance" PRIMARY KEY ("UserId", "TemplateInstanceId", "DefaultTagId");


ALTER TABLE ONLY public."Template"
    ADD CONSTRAINT "PK_Template" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."TemplateInstance"
    ADD CONSTRAINT "PK_TemplateInstance" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."User"
    ADD CONSTRAINT "PK_User" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."User_TemplateInstance"
    ADD CONSTRAINT "PK_User_TemplateInstance" PRIMARY KEY ("UserId", "TemplateInstanceId");


ALTER TABLE ONLY public."Vote_CommentCard"
    ADD CONSTRAINT "PK_Vote_CommentCard" PRIMARY KEY ("CommentCardId", "UserId");


ALTER TABLE ONLY public."Vote_CommentTemplate"
    ADD CONSTRAINT "PK_Vote_CommentTemplate" PRIMARY KEY ("CommentTemplateId", "UserId");


ALTER TABLE ONLY public."Vote_Feedback"
    ADD CONSTRAINT "PK_Vote_Feedback" PRIMARY KEY ("FeedbackId", "UserId");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "UQ_AcquiredCard_UserId_BranchId" UNIQUE ("UserId", "BranchId");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "UQ_BranchInstance_BranchId_Id" UNIQUE ("BranchId", "Id");


ALTER TABLE ONLY public."Branch"
    ADD CONSTRAINT "UQ_Branch_CardId_Id" UNIQUE ("CardId", "Id");


CREATE INDEX "IX_AcquiredCard_BranchInstanceId" ON public."AcquiredCard" USING btree ("BranchInstanceId");


CREATE INDEX "IX_AcquiredCard_CardSettingId" ON public."AcquiredCard" USING btree ("CardSettingId");


CREATE INDEX "IX_AcquiredCard_CardState" ON public."AcquiredCard" USING btree ("CardState");


CREATE INDEX "IX_AcquiredCard_UserId" ON public."AcquiredCard" USING btree ("UserId");


CREATE UNIQUE INDEX "IX_AcquiredCard_UserId_BranchInstanceId" ON public."AcquiredCard" USING btree ("UserId", "BranchInstanceId");


CREATE UNIQUE INDEX "IX_AcquiredCard_UserId_CardId" ON public."AcquiredCard" USING btree ("UserId", "CardId");


CREATE UNIQUE INDEX "IX_AlphaBetaKey_Key" ON public."AlphaBetaKey" USING btree ("Key");


CREATE INDEX "IX_BranchInstance_BranchId" ON public."BranchInstance" USING btree ("BranchId");


CREATE INDEX "IX_BranchInstance_Hash" ON public."BranchInstance" USING btree ("Hash");


CREATE INDEX "IX_BranchInstance_TemplateInstanceId" ON public."BranchInstance" USING btree ("TemplateInstanceId");


CREATE INDEX "IX_CardSetting_UserId" ON public."CardSetting" USING btree ("UserId");


CREATE INDEX "IX_Card_AuthorId" ON public."Card" USING btree ("AuthorId");


CREATE INDEX "IX_CommentCard_CardId" ON public."CommentCard" USING btree ("CardId");


CREATE INDEX "IX_CommentCard_UserId" ON public."CommentCard" USING btree ("UserId");


CREATE INDEX "IX_CommentTemplate_TemplateId" ON public."CommentTemplate" USING btree ("TemplateId");


CREATE INDEX "IX_CommentTemplate_UserId" ON public."CommentTemplate" USING btree ("UserId");


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


CREATE INDEX "IX_Tag_User_TemplateInstance_DefaultTagId" ON public."Tag_User_TemplateInstance" USING btree ("DefaultTagId");


CREATE INDEX "IX_TemplateInstance_Hash" ON public."TemplateInstance" USING btree ("Hash");


CREATE INDEX "IX_TemplateInstance_TemplateId" ON public."TemplateInstance" USING btree ("TemplateId");


CREATE INDEX "IX_Template_AuthorId" ON public."Template" USING btree ("AuthorId");


CREATE INDEX "IX_User_TemplateInstance_DefaultCardSettingId" ON public."User_TemplateInstance" USING btree ("DefaultCardSettingId");


CREATE INDEX "IX_User_TemplateInstance_TemplateInstanceId" ON public."User_TemplateInstance" USING btree ("TemplateInstanceId");


CREATE INDEX "IX_Vote_CommentCard_UserId" ON public."Vote_CommentCard" USING btree ("UserId");


CREATE INDEX "IX_Vote_CommentTemplate_UserId" ON public."Vote_CommentTemplate" USING btree ("UserId");


CREATE INDEX "IX_Vote_Feedback_UserId" ON public."Vote_Feedback" USING btree ("UserId");


CREATE INDEX idx_fts_branchinstance_tsvector ON public."BranchInstance" USING gin ("TsVector");


CREATE INDEX idx_fts_communalfieldinstance_tsvector ON public."CommunalFieldInstance" USING gin ("TsVector");


CREATE INDEX idx_fts_relationship_tsvector ON public."Relationship" USING gin ("TsVector");


CREATE INDEX idx_fts_tag_tsvector ON public."Tag" USING gin ("TsVector");


CREATE INDEX idx_fts_templateinstance_tsvector ON public."TemplateInstance" USING gin ("TsVector");


CREATE TRIGGER tr_branch_afterinsert AFTER INSERT ON public."Branch" FOR EACH ROW EXECUTE FUNCTION public.fn_branch_afterinsert();
CREATE TRIGGER tr_acquiredcard_afterinsertdeleteupdate AFTER INSERT OR DELETE OR UPDATE ON public."AcquiredCard" FOR EACH ROW EXECUTE FUNCTION public.fn_acquiredcard_afterinsertdeleteupdate();


CREATE TRIGGER tr_acquiredcard_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."AcquiredCard" FOR EACH ROW EXECUTE FUNCTION public.fn_acquiredcard_beforeinsertupdate();


CREATE TRIGGER tr_branchinstance_beforeinsert BEFORE INSERT ON public."BranchInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_branchinstance_beforeinsert();


CREATE TRIGGER tr_communalfieldinstance_beforeinsert BEFORE INSERT ON public."CommunalFieldInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_communalfieldinstance_beforeinsert();


CREATE TRIGGER tr_relationship_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."Relationship" FOR EACH ROW EXECUTE FUNCTION public.fn_relationship_beforeinsertupdate();


CREATE TRIGGER tr_tag_beforeinsertupdate BEFORE INSERT OR UPDATE ON public."Tag" FOR EACH ROW EXECUTE FUNCTION public.fn_tag_beforeinsertupdate();


CREATE TRIGGER tr_templateinstance_beforeinsert BEFORE INSERT ON public."TemplateInstance" FOR EACH ROW EXECUTE FUNCTION public.fn_templateinstance_beforeinsert();


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
    ADD CONSTRAINT "FK_AcquiredCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "FK_BranchInstance_Branch_BranchId" FOREIGN KEY ("BranchId") REFERENCES public."Branch"("Id");


ALTER TABLE ONLY public."BranchInstance"
    ADD CONSTRAINT "FK_BranchInstance_TemplateInstance_TemplateInstanceId" FOREIGN KEY ("TemplateInstanceId") REFERENCES public."TemplateInstance"("Id");


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


ALTER TABLE ONLY public."CommunalFieldInstance_BranchInstance"
    ADD CONSTRAINT "FK_CommFieldInst_CardInst_CommFieldInst_CommFieldInstId" FOREIGN KEY ("CommunalFieldInstanceId") REFERENCES public."CommunalFieldInstance"("Id");


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "FK_CommentCard_Card_CardId" FOREIGN KEY ("CardId") REFERENCES public."Card"("Id");


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "FK_CommentCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommentTemplate"
    ADD CONSTRAINT "FK_CommentTemplate_Template_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."Template"("Id");


ALTER TABLE ONLY public."CommentTemplate"
    ADD CONSTRAINT "FK_CommentTemplate_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance_BranchInstance"
    ADD CONSTRAINT "FK_CommunalFieldInst_CardInst_CardInst_CardInstId" FOREIGN KEY ("BranchInstanceId") REFERENCES public."BranchInstance"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance"
    ADD CONSTRAINT "FK_CommunalFieldInstance_CommunalField_CommunalFieldId" FOREIGN KEY ("CommunalFieldId") REFERENCES public."CommunalField"("Id");


ALTER TABLE ONLY public."CommunalField"
    ADD CONSTRAINT "FK_CommunalField_CommunalFieldInstance_LatestInstanceId" FOREIGN KEY ("LatestInstanceId") REFERENCES public."CommunalFieldInstance"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."CommunalField"
    ADD CONSTRAINT "FK_CommunalField_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


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


ALTER TABLE ONLY public."Tag_AcquiredCard"
    ADD CONSTRAINT "FK_Tag_AcquiredCard_AcquiredCard_AcquiredCardId" FOREIGN KEY ("AcquiredCardId") REFERENCES public."AcquiredCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."Tag_AcquiredCard"
    ADD CONSTRAINT "FK_Tag_AcquiredCard_Tag_TagId" FOREIGN KEY ("TagId") REFERENCES public."Tag"("Id");


ALTER TABLE ONLY public."Tag_User_TemplateInstance"
    ADD CONSTRAINT "FK_Tag_User_TemplatInst_User_TemplatInst_UserId_TemplatInstId" FOREIGN KEY ("UserId", "TemplateInstanceId") REFERENCES public."User_TemplateInstance"("UserId", "TemplateInstanceId");


ALTER TABLE ONLY public."Tag_User_TemplateInstance"
    ADD CONSTRAINT "FK_Tag_User_TemplateInstance_Tag_DefaultTagId" FOREIGN KEY ("DefaultTagId") REFERENCES public."Tag"("Id");


ALTER TABLE ONLY public."TemplateInstance"
    ADD CONSTRAINT "FK_TemplateInstance_Template_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."Template"("Id");


ALTER TABLE ONLY public."Template"
    ADD CONSTRAINT "FK_Template_TemplateInstance_LatestInstanceId" FOREIGN KEY ("LatestInstanceId") REFERENCES public."TemplateInstance"("Id") DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE ONLY public."Template"
    ADD CONSTRAINT "FK_Template_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."User"
    ADD CONSTRAINT "FK_User_CardSetting_DefaultCardSettingId" FOREIGN KEY ("DefaultCardSettingId") REFERENCES public."CardSetting"("Id");


ALTER TABLE ONLY public."User_TemplateInstance"
    ADD CONSTRAINT "FK_User_TemplateInstance_CardSetting_DefaultCardSettingId" FOREIGN KEY ("DefaultCardSettingId") REFERENCES public."CardSetting"("Id");


ALTER TABLE ONLY public."User_TemplateInstance"
    ADD CONSTRAINT "FK_User_TemplateInstance_TemplateInstance_TemplateInstanceId" FOREIGN KEY ("TemplateInstanceId") REFERENCES public."TemplateInstance"("Id");


ALTER TABLE ONLY public."User_TemplateInstance"
    ADD CONSTRAINT "FK_User_TemplateInstance_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Vote_CommentCard"
    ADD CONSTRAINT "FK_Vote_CommentCard_CommentCard_CommentCardId" FOREIGN KEY ("CommentCardId") REFERENCES public."CommentCard"("Id");


ALTER TABLE ONLY public."Vote_CommentCard"
    ADD CONSTRAINT "FK_Vote_CommentCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Vote_CommentTemplate"
    ADD CONSTRAINT "FK_Vote_CommentTemplate_CommentTemplate_CommentTemplateId" FOREIGN KEY ("CommentTemplateId") REFERENCES public."CommentTemplate"("Id");


ALTER TABLE ONLY public."Vote_CommentTemplate"
    ADD CONSTRAINT "FK_Vote_CommentTemplate_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Vote_Feedback"
    ADD CONSTRAINT "FK_Vote_Feedback_Feedback_FeedbackId" FOREIGN KEY ("FeedbackId") REFERENCES public."Feedback"("Id");


ALTER TABLE ONLY public."Vote_Feedback"
    ADD CONSTRAINT "FK_Vote_Feedback_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");



