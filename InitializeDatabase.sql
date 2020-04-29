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

CREATE FUNCTION public.cardinstance_tsvectorfunction() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  IF (NEW."TsVectorHelper" IS NOT NULL) THEN
    NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."TsVectorHelper");
    NEW."TsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.cardinstance_tsvectorfunction() OWNER TO postgres;

CREATE FUNCTION public.communalfieldinstance_tsvectorfunction() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  IF (NEW."BWeightTsVectorHelper" IS NOT NULL) THEN
    NEW."TsVector" =
        setweight(to_tsvector('pg_catalog.english', NEW."FieldName"), 'A') ||
        setweight(to_tsvector('pg_catalog.english', NEW."BWeightTsVectorHelper"), 'B');
    NEW."BWeightTsVectorHelper" = NULL;
  END IF;
  return NEW;
end  
$$;


ALTER FUNCTION public.communalfieldinstance_tsvectorfunction() OWNER TO postgres;

CREATE FUNCTION public.relationship_tsvectorfunction() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
begin
  NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."Name");
  return NEW;
end
$$;


ALTER FUNCTION public.relationship_tsvectorfunction() OWNER TO postgres;

CREATE FUNCTION public.tag_tsvectorfunction() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
  NEW."TsVector" = to_tsvector('pg_catalog.english', NEW."Name");
  return NEW;
end
$$;


ALTER FUNCTION public.tag_tsvectorfunction() OWNER TO postgres;

CREATE FUNCTION public.templateinstance_tsvectorfunction() RETURNS trigger
    LANGUAGE plpgsql
    AS $$  
begin
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


ALTER FUNCTION public.templateinstance_tsvectorfunction() OWNER TO postgres;

CREATE FUNCTION public.trigger_to_update_userscount_of_card_and_cardinstance() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
	BEGIN
		IF (TG_OP = 'DELETE' OR TG_OP = 'UPDATE') THEN
			UPDATE	"CardInstance" ci
			SET		"Users" = ( SELECT Count(*)
								FROM "AcquiredCard"
								WHERE "CardInstanceId" = OLD."CardInstanceId" AND "CardState" <> 3 )
			WHERE	ci."Id" = OLD."CardInstanceId";
						UPDATE	"Card" c
			SET		"Users" = ( SELECT	COALESCE(SUM(ci2."Users"), 0)
								FROM	"CardInstance" ci2
								WHERE	ci2."CardId" = c."Id" )
			FROM	"CardInstance" ci
			WHERE	ci."Id" = OLD."CardInstanceId";
		END IF;
		IF (TG_OP = 'INSERT' OR TG_OP = 'UPDATE') THEN
			UPDATE	"CardInstance" ci
			SET		"Users" = ( SELECT Count(*)
								FROM "AcquiredCard"
								WHERE "CardInstanceId" = NEW."CardInstanceId" AND "CardState" <> 3 )
			WHERE	ci."Id" = NEW."CardInstanceId";
						UPDATE	"Card" c
			SET		"Users" = ( SELECT	COALESCE(SUM(ci2."Users"), 0)
								FROM	"CardInstance" ci2
								WHERE	ci2."CardId" = c."Id" )
			FROM	"CardInstance" ci
			WHERE	ci."Id" = NEW."CardInstanceId";
		END IF;
		RETURN NULL;
	END;
$$;


ALTER FUNCTION public.trigger_to_update_userscount_of_card_and_cardinstance() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE TABLE public."AcquiredCard" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "CardId" integer NOT NULL,
    "CardInstanceId" integer NOT NULL,
    "CardState" smallint NOT NULL,
    "EaseFactorInPermille" smallint NOT NULL,
    "IntervalOrStepsIndex" smallint NOT NULL,
    "Due" timestamp without time zone NOT NULL,
    "CardSettingId" integer NOT NULL,
    "IsLapsed" boolean NOT NULL
);


ALTER TABLE public."AcquiredCard" OWNER TO postgres;

CREATE TABLE public."Card" (
    "Id" integer NOT NULL,
    "AuthorId" integer NOT NULL,
    "Users" integer NOT NULL,
    "CopySourceId" integer,
    "BranchSourceId" integer,
    CONSTRAINT at_most_one_source CHECK (((("CopySourceId" IS NULL) AND ("BranchSourceId" IS NULL)) OR (("CopySourceId" IS NOT NULL) AND ("BranchSourceId" IS NULL)) OR (("CopySourceId" IS NULL) AND ("BranchSourceId" IS NOT NULL))))
);


ALTER TABLE public."Card" OWNER TO postgres;

CREATE TABLE public."CardInstance" (
    "Id" integer NOT NULL,
    "Created" timestamp without time zone NOT NULL,
    "Modified" timestamp without time zone,
    "CardId" integer NOT NULL,
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
    CONSTRAINT "CardInstance_TsVectorHelper_IsNull" CHECK (("TsVectorHelper" IS NULL))
);


ALTER TABLE public."CardInstance" OWNER TO postgres;

CREATE VIEW public."LatestCardInstance" AS
 SELECT c."AuthorId",
    c."Users" AS "CardUsers",
    i1."Id" AS "CardInstanceId",
    i1."CardId",
    i1."Created",
    i1."Modified",
    i1."IsDmca",
    i1."FieldValues",
    i1."TemplateInstanceId",
    i1."Users" AS "InstanceUsers",
    i1."EditSummary",
    i1."AnkiNoteId",
    i1."AnkiNoteOrd"
   FROM ((public."Card" c
     JOIN public."CardInstance" i1 ON ((c."Id" = i1."CardId")))
     LEFT JOIN public."CardInstance" i2 ON (((c."Id" = i2."CardId") AND ((i1."Created" < i2."Created") OR ((i1."Created" = i2."Created") AND (i1."Id" < i2."Id"))))))
  WHERE (i2."Id" IS NULL);


ALTER TABLE public."LatestCardInstance" OWNER TO postgres;

CREATE VIEW public."AcquiredCardIsLatest" AS
 SELECT a."Id",
    a."UserId",
    a."CardInstanceId",
    a."CardState",
    a."EaseFactorInPermille",
    a."IntervalOrStepsIndex",
    a."Due",
    a."CardSettingId",
    a."IsLapsed",
    (l."CardInstanceId" IS NULL) AS "IsLatest"
   FROM (public."AcquiredCard" a
     LEFT JOIN public."LatestCardInstance" l ON ((l."CardInstanceId" = a."CardInstanceId")));


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

CREATE VIEW public."CardInstanceRelationshipCount" AS
 SELECT sac."CardInstanceId" AS "SourceCardInstanceId",
    tac."CardInstanceId" AS "TargetCardInstanceId",
    unnest(ARRAY[sac."CardInstanceId", tac."CardInstanceId"]) AS "CardInstanceId",
    ( SELECT r."Name"
           FROM public."Relationship" r
          WHERE (r."Id" = rac."RelationshipId")
         LIMIT 1) AS "Name",
    count(*) AS "Count"
   FROM ((public."Relationship_AcquiredCard" rac
     JOIN public."AcquiredCard" sac ON ((rac."SourceAcquiredCardId" = sac."Id")))
     JOIN public."AcquiredCard" tac ON ((rac."TargetAcquiredCardId" = tac."Id")))
  GROUP BY sac."CardInstanceId", tac."CardInstanceId", rac."RelationshipId";


ALTER TABLE public."CardInstanceRelationshipCount" OWNER TO postgres;

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

CREATE VIEW public."CardInstanceTagCount" AS
 SELECT i."Id" AS "CardInstanceId",
    ( SELECT t."Name"
           FROM public."Tag" t
          WHERE (t."Id" = ta."TagId")
         LIMIT 1) AS "Name",
    count(*) AS "Count"
   FROM ((public."CardInstance" i
     JOIN public."AcquiredCard" ac ON ((ac."CardInstanceId" = i."Id")))
     JOIN public."Tag_AcquiredCard" ta ON ((ta."AcquiredCardId" = ac."Id")))
  GROUP BY i."Id", ta."TagId";


ALTER TABLE public."CardInstanceTagCount" OWNER TO postgres;

ALTER TABLE public."CardInstance" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."CardInstance_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


CREATE VIEW public."CardRelationshipCount" AS
 SELECT si."CardId" AS "SourceCardId",
    ti."CardId" AS "TargetCardId",
    unnest(ARRAY[si."CardId", ti."CardId"]) AS "CardId",
    ( SELECT r."Name"
           FROM public."Relationship" r
          WHERE (r."Id" = rac."RelationshipId")
         LIMIT 1) AS "Name",
    count(*) AS "Count"
   FROM ((((public."Relationship_AcquiredCard" rac
     JOIN public."AcquiredCard" sac ON ((rac."SourceAcquiredCardId" = sac."Id")))
     JOIN public."AcquiredCard" tac ON ((rac."TargetAcquiredCardId" = tac."Id")))
     JOIN public."CardInstance" si ON ((sac."CardInstanceId" = si."Id")))
     JOIN public."CardInstance" ti ON ((tac."CardInstanceId" = ti."Id")))
  GROUP BY si."CardId", ti."CardId", rac."RelationshipId";


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
   FROM (((public."Card" c
     JOIN public."CardInstance" i ON ((c."Id" = i."CardId")))
     JOIN public."AcquiredCard" ac ON ((ac."CardInstanceId" = i."Id")))
     JOIN public."Tag_AcquiredCard" ta ON ((ta."AcquiredCardId" = ac."Id")))
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
    "AuthorId" integer NOT NULL
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

CREATE TABLE public."CommunalFieldInstance_CardInstance" (
    "CardInstanceId" integer NOT NULL,
    "CommunalFieldInstanceId" integer NOT NULL
);


ALTER TABLE public."CommunalFieldInstance_CardInstance" OWNER TO postgres;

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

CREATE TABLE public."File_CardInstance" (
    "CardInstanceId" integer NOT NULL,
    "FileId" integer NOT NULL
);


ALTER TABLE public."File_CardInstance" OWNER TO postgres;

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


CREATE VIEW public."LatestCommunalFieldInstance" AS
 SELECT c."AuthorId",
    i1."Id" AS "CommunalFieldInstanceId",
    i1."CommunalFieldId",
    i1."FieldName",
    i1."Value",
    i1."Created",
    i1."Modified",
    i1."EditSummary"
   FROM ((public."CommunalField" c
     JOIN public."CommunalFieldInstance" i1 ON ((c."Id" = i1."CommunalFieldId")))
     LEFT JOIN public."CommunalFieldInstance" i2 ON (((c."Id" = i2."CommunalFieldId") AND ((i1."Created" < i2."Created") OR ((i1."Created" = i2."Created") AND (i1."Id" < i2."Id"))))))
  WHERE (i2."Id" IS NULL);


ALTER TABLE public."LatestCommunalFieldInstance" OWNER TO postgres;

CREATE TABLE public."Template" (
    "Id" integer NOT NULL,
    "AuthorId" integer NOT NULL
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

CREATE VIEW public."LatestTemplateInstance" AS
 SELECT t."AuthorId",
    i1."Id" AS "TemplateInstanceId",
    i1."TemplateId",
    i1."Name",
    i1."Css",
    i1."Created",
    i1."Modified",
    i1."LatexPre",
    i1."LatexPost",
    i1."IsDmca",
    i1."QuestionTemplate",
    i1."AnswerTemplate",
    i1."ShortQuestionTemplate",
    i1."ShortAnswerTemplate",
    i1."Fields",
    i1."EditSummary",
    i1."AnkiId"
   FROM ((public."Template" t
     JOIN public."TemplateInstance" i1 ON ((t."Id" = i1."TemplateId")))
     LEFT JOIN public."TemplateInstance" i2 ON (((t."Id" = i2."TemplateId") AND ((i1."Created" < i2."Created") OR ((i1."Created" = i2."Created") AND (i1."Id" < i2."Id"))))))
  WHERE (i2."Id" IS NULL);


ALTER TABLE public."LatestTemplateInstance" OWNER TO postgres;

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


































INSERT INTO public."Template" ("Id", "AuthorId") VALUES (1, 2);
INSERT INTO public."Template" ("Id", "AuthorId") VALUES (2, 2);
INSERT INTO public."Template" ("Id", "AuthorId") VALUES (3, 2);
INSERT INTO public."Template" ("Id", "AuthorId") VALUES (4, 2);
INSERT INTO public."Template" ("Id", "AuthorId") VALUES (5, 2);


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

{{Back}}', '', '', 'FrontArial20False0FalseBackArial20False1False', 'Imported from Anki', 1554689669581, B'11010011010100000110000000001000110110100011010001111101011101001011010001111011100111100000111101011000000100011100110101101100011010110001111101011001110111000011111001111010110001001010111001111100100111111111010000000011110110100111011101111111011111110011111101100111110110011011100010010001001010111101000000010100101101101010011111101100011011110100000111111100001110000001100011001101111101101111111011110010111010001000100110100100000110000011011111010111010110110101100110110111100100010011010101111101', NULL, '''20px'':15 ''align'':18 ''arial'':11 ''back'':3C,6C ''background'':23 ''background-color'':22 ''basic'':1A ''black'':21 ''card'':7 ''center'':19 ''color'':20,24 ''famili'':10 ''font'':9,13 ''font-famili'':8 ''font-siz'':12 ''front'':2C,4C ''frontsid'':5C ''size'':14 ''text'':17 ''text-align'':16 ''white'':25');
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

{{Back}}', '', '', 'FrontArial20False0FalseBackArial20False1False', 'Imported from Anki', 1554689669577, B'00011110101001100011011110011010100011100000000110001101000000001111100001111000101000000110000011001001010000011101001010000100101100000111011100010110011110100011110111110111010111000011011110111000000000011111111101111100110000011110011110011011001110110110011110100101011000010110111100111101111000111000111001010101101110110001001001110011100111101100000011111011101110011111101000110101111001101011110011011000101111110101101010111101111100001010010100000101101110010010001010110011001001111111000111011101', NULL, '''1'':6A ''20px'':20 ''align'':23 ''arial'':16 ''back'':8C,11C ''background'':28 ''background-color'':27 ''basic'':1A ''black'':26 ''card'':4A,5A,12 ''center'':24 ''color'':25,29 ''famili'':15 ''font'':14,18 ''font-famili'':13 ''font-siz'':17 ''front'':7C,9C ''frontsid'':10C ''revers'':3A ''size'':19 ''text'':22 ''text-align'':21 ''white'':30');
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

{{Back}}', '', '', 'FrontArial20False0FalseBackArial20False1FalseAdd ReverseArial20False2False', 'Imported from Anki', 1554689669572, B'01111101011000000100011000000000011011111010001011000100110110000101100100100101101111101011001000001111000101111100000010011111011110011000100011101001011011101011101001010011001101010101101110100010011101110111101000000000100101000100111110000001111111000010000000000010000111001101000101110010100110010101011101001110110001011101010000001101010111010110100000011011000111110111101000000010011000110010100010011001011101011011001110100110101001011010101100110011011100000001010001011010100010010101011011110001', NULL, '''1'':6A ''20px'':22 ''add'':9C ''align'':25 ''arial'':18 ''back'':8C,13C ''background'':30 ''background-color'':29 ''basic'':1A ''black'':28 ''card'':4A,5A,14 ''center'':26 ''color'':27,31 ''famili'':17 ''font'':16,20 ''font-famili'':15 ''font-siz'':19 ''front'':7C,11C ''frontsid'':12C ''option'':2A ''revers'':3A,10C ''size'':21 ''text'':24 ''text-align'':23 ''white'':32');
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

{{Back}}', '', '', 'FrontArial20False0FalseBackArial20False1False', 'Imported from Anki', 1587663611565, B'10111000110000111110101000101010101001111110101010001010100101101100101110000111110001011111111100000001110010000111110101010000110110110000000111010111101110001110010110111101001011101000100100000000000011110100100110011110101000010001011111010010100110000011110110000001111001010001110111111001110001001011010010101010100011010111111010110100000000110001111101110011101110000011011101010001000100100011111011101110111101011011100111110101110100101011010011010100110011111100011000100100101000101100110001100100', NULL, '''20px'':21 ''align'':24 ''answer'':5A ''arial'':17 ''back'':7C,10C,12C ''background'':29 ''background-color'':28 ''basic'':1A ''black'':27 ''card'':13 ''center'':25 ''color'':26,30 ''famili'':16 ''font'':15,19 ''font-famili'':14 ''font-siz'':18 ''front'':6C,8C ''frontsid'':11C ''size'':20 ''text'':23 ''text-align'':22 ''type'':2A,9C ''white'':31');
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
{{Extra}}', '', '', 'TextArial20False0FalseExtraArial20False1False', 'Imported from Anki', 1554689669570, B'00111100110000100110001101111111111000100000110110101001001010110100011110000111101101001011000110001101100111011000100000001000111000110000010000001001101101011100110111000001101111110010000000111111101101100011011001100001010100011001101001111000000101100000001000101011111111100010111011000111100000001100000011001010110011001010010101011010010111100110100000111011011101101011101010100101100101110010011100111111011101011000000100100011011001110111001001100110110000110101001011000000111101010101010101000011', NULL, '''20px'':17 ''align'':20 ''arial'':13 ''background'':25 ''background-color'':24 ''black'':23 ''blue'':34 ''bold'':32 ''card'':9 ''center'':21 ''cloze'':1A,4C,6C,28,36 ''color'':22,26,33,37 ''extra'':3C,8C ''famili'':12 ''font'':11,15,30 ''font-famili'':10 ''font-siz'':14 ''font-weight'':29 ''lightblu'':38 ''nightmod'':35 ''size'':16 ''text'':2C,5C,7C,19 ''text-align'':18 ''weight'':31 ''white'':27');
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

{{type:Back}}', '', '', 'FrontArial20False0FalseBackArial20False1False', 'Answer uses {{Front}} instead of {{FrontSide}} and {{type:Back}} instead of {{Back}} as of Anki v2.1.15', 1587486094455, B'01001101011100001111110011111111001000110010100010001100111010101100000011001010001110111010101111111100000110100001001110011001011001011010011100110100100011111111100011100101111000110110000011011110001101110101101001001100100100111110111001110001110010101001000011000111001000010101111010110011111101111010101100000000111001110010101111111110101110011111110110000111101011010110111000010011111101110110010111101000101111100100100110111111010101101101111110111100000101011001101011011011110011100111000011111001', NULL, '''20px'':22 ''align'':25 ''answer'':5A ''arial'':18 ''back'':7C,10C,13C ''background'':30 ''background-color'':29 ''basic'':1A ''black'':28 ''card'':14 ''center'':26 ''color'':27,31 ''famili'':17 ''font'':16,20 ''font-famili'':15 ''font-siz'':19 ''front'':6C,8C,11C ''size'':21 ''text'':24 ''text-align'':23 ''type'':2A,9C,12C ''white'':32');
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
', '\end{document}', false, '{{cloze:Text}}', '{{cloze:Text}}', '', '', 'TextArial20False0False', '"Extra" field removed in Anki v2.1.22', 1587670846820, B'10000100000010110101111110100010011001101110000011110001110101011000011001010000110110000110000001011101110111011111100101110101100100010111100001011101101011110101000011100111001100101111000000101000010110110010110011010111110101110101100101010010110010001000010001000111000000000010100110011101110111111100110010011001011100011010111010111100111110000100001011101011001100100000001111111011110100000111001010111010001010100001111110011010011111100100010100011011100010011001000110110111101101010001100100100001', NULL, '''20px'':15 ''align'':18 ''arial'':11 ''background'':23 ''background-color'':22 ''black'':21 ''blue'':32 ''bold'':30 ''card'':7 ''center'':19 ''cloze'':1A,3C,5C,26,34 ''color'':20,24,31,35 ''famili'':10 ''font'':9,13,28 ''font-famili'':8 ''font-siz'':12 ''font-weight'':27 ''lightblu'':36 ''nightmod'':33 ''size'':14 ''text'':2C,4C,6C,17 ''text-align'':16 ''weight'':29 ''white'':25');


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


SELECT pg_catalog.setval('public."CardInstance_Id_seq"', 1001, false);


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


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "PK_Card" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."CardInstance"
    ADD CONSTRAINT "PK_CardInstance" PRIMARY KEY ("Id");


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


ALTER TABLE ONLY public."CommunalFieldInstance_CardInstance"
    ADD CONSTRAINT "PK_CommunalFieldInstance_CardInstance" PRIMARY KEY ("CommunalFieldInstanceId", "CardInstanceId");


ALTER TABLE ONLY public."Feedback"
    ADD CONSTRAINT "PK_Feedback" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."File"
    ADD CONSTRAINT "PK_File" PRIMARY KEY ("Id");


ALTER TABLE ONLY public."File_CardInstance"
    ADD CONSTRAINT "PK_File_CardInstance" PRIMARY KEY ("CardInstanceId", "FileId");


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


ALTER TABLE ONLY public."CardInstance"
    ADD CONSTRAINT "UQ_CardInstance_CardId_Id" UNIQUE ("CardId", "Id");


CREATE INDEX "IX_AcquiredCard_CardInstanceId" ON public."AcquiredCard" USING btree ("CardInstanceId");


CREATE INDEX "IX_AcquiredCard_CardSettingId" ON public."AcquiredCard" USING btree ("CardSettingId");


CREATE INDEX "IX_AcquiredCard_CardState" ON public."AcquiredCard" USING btree ("CardState");


CREATE INDEX "IX_AcquiredCard_UserId" ON public."AcquiredCard" USING btree ("UserId");


CREATE UNIQUE INDEX "IX_AcquiredCard_UserId_CardId" ON public."AcquiredCard" USING btree ("UserId", "CardId");


CREATE UNIQUE INDEX "IX_AcquiredCard_UserId_CardInstanceId" ON public."AcquiredCard" USING btree ("UserId", "CardInstanceId");


CREATE UNIQUE INDEX "IX_AlphaBetaKey_Key" ON public."AlphaBetaKey" USING btree ("Key");


CREATE INDEX "IX_CardInstance_CardId" ON public."CardInstance" USING btree ("CardId");


CREATE INDEX "IX_CardInstance_Hash" ON public."CardInstance" USING btree ("Hash");


CREATE INDEX "IX_CardInstance_TemplateInstanceId" ON public."CardInstance" USING btree ("TemplateInstanceId");


CREATE INDEX "IX_CardSetting_UserId" ON public."CardSetting" USING btree ("UserId");


CREATE INDEX "IX_Card_AuthorId" ON public."Card" USING btree ("AuthorId");


CREATE INDEX "IX_CommentCard_CardId" ON public."CommentCard" USING btree ("CardId");


CREATE INDEX "IX_CommentCard_UserId" ON public."CommentCard" USING btree ("UserId");


CREATE INDEX "IX_CommentTemplate_TemplateId" ON public."CommentTemplate" USING btree ("TemplateId");


CREATE INDEX "IX_CommentTemplate_UserId" ON public."CommentTemplate" USING btree ("UserId");


CREATE INDEX "IX_CommunalFieldInstance_CardInstance_CardInstanceId" ON public."CommunalFieldInstance_CardInstance" USING btree ("CardInstanceId");


CREATE INDEX "IX_CommunalFieldInstance_CommunalFieldId" ON public."CommunalFieldInstance" USING btree ("CommunalFieldId");


CREATE INDEX "IX_CommunalField_AuthorId" ON public."CommunalField" USING btree ("AuthorId");


CREATE INDEX "IX_Feedback_ParentId" ON public."Feedback" USING btree ("ParentId");


CREATE INDEX "IX_Feedback_UserId" ON public."Feedback" USING btree ("UserId");


CREATE INDEX "IX_File_CardInstance_FileId" ON public."File_CardInstance" USING btree ("FileId");


CREATE UNIQUE INDEX "IX_File_Sha256" ON public."File" USING btree ("Sha256");


CREATE INDEX "IX_Filter_UserId" ON public."Filter" USING btree ("UserId");


CREATE INDEX "IX_History_AcquiredCardId" ON public."History" USING btree ("AcquiredCardId");


CREATE INDEX "IX_Relationship_AcquiredCard_RelationshipId" ON public."Relationship_AcquiredCard" USING btree ("RelationshipId");


CREATE INDEX "IX_Relationship_AcquiredCard_TargetAcquiredCardId" ON public."Relationship_AcquiredCard" USING btree ("TargetAcquiredCardId");


CREATE UNIQUE INDEX "IX_Relationship_Name" ON public."Relationship" USING btree ("Name");


CREATE INDEX "IX_Tag_AcquiredCard_AcquiredCardId" ON public."Tag_AcquiredCard" USING btree ("AcquiredCardId");


CREATE UNIQUE INDEX "IX_Tag_Name" ON public."Tag" USING btree ("Name");


CREATE INDEX "IX_Tag_User_TemplateInstance_DefaultTagId" ON public."Tag_User_TemplateInstance" USING btree ("DefaultTagId");


CREATE INDEX "IX_TemplateInstance_Hash" ON public."TemplateInstance" USING btree ("Hash");


CREATE INDEX "IX_TemplateInstance_TemplateId" ON public."TemplateInstance" USING btree ("TemplateId");


CREATE INDEX "IX_Template_AuthorId" ON public."Template" USING btree ("AuthorId");


CREATE UNIQUE INDEX "IX_User_DisplayName" ON public."User" USING btree ("DisplayName");


CREATE INDEX "IX_User_TemplateInstance_DefaultCardSettingId" ON public."User_TemplateInstance" USING btree ("DefaultCardSettingId");


CREATE INDEX "IX_User_TemplateInstance_TemplateInstanceId" ON public."User_TemplateInstance" USING btree ("TemplateInstanceId");


CREATE INDEX "IX_Vote_CommentCard_UserId" ON public."Vote_CommentCard" USING btree ("UserId");


CREATE INDEX "IX_Vote_CommentTemplate_UserId" ON public."Vote_CommentTemplate" USING btree ("UserId");


CREATE INDEX "IX_Vote_Feedback_UserId" ON public."Vote_Feedback" USING btree ("UserId");


CREATE INDEX idx_fts_cardinstance_tsvector ON public."CardInstance" USING gin ("TsVector");


CREATE INDEX idx_fts_communalfieldinstance_tsvector ON public."CommunalFieldInstance" USING gin ("TsVector");


CREATE INDEX idx_fts_relationship_tsvector ON public."Relationship" USING gin ("TsVector");


CREATE INDEX idx_fts_tag_tsvector ON public."Tag" USING gin ("TsVector");


CREATE INDEX idx_fts_templateinstance_tsvector ON public."TemplateInstance" USING gin ("TsVector");


CREATE TRIGGER cardinstance_tsvectortrigger BEFORE INSERT OR UPDATE ON public."CardInstance" FOR EACH ROW EXECUTE FUNCTION public.cardinstance_tsvectorfunction();


CREATE TRIGGER communalfieldinstance_tsvectortrigger BEFORE INSERT OR UPDATE ON public."CommunalFieldInstance" FOR EACH ROW EXECUTE FUNCTION public.communalfieldinstance_tsvectorfunction();


CREATE TRIGGER relationship_tsvectortrigger BEFORE INSERT OR UPDATE ON public."Relationship" FOR EACH ROW EXECUTE FUNCTION public.relationship_tsvectorfunction();


CREATE TRIGGER tag_tsvectortrigger BEFORE INSERT OR UPDATE ON public."Tag" FOR EACH ROW EXECUTE FUNCTION public.tag_tsvectorfunction();


CREATE TRIGGER templateinstance_tsvectortrigger BEFORE INSERT OR UPDATE ON public."TemplateInstance" FOR EACH ROW EXECUTE FUNCTION public.templateinstance_tsvectorfunction();


CREATE TRIGGER trigger_to_update_userscount_of_card_and_cardinstance AFTER INSERT OR DELETE OR UPDATE ON public."AcquiredCard" FOR EACH ROW EXECUTE FUNCTION public.trigger_to_update_userscount_of_card_and_cardinstance();


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_CardInstance_CardId_CardInstanceId" FOREIGN KEY ("CardId", "CardInstanceId") REFERENCES public."CardInstance"("CardId", "Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_CardInstance_CardInstanceId" FOREIGN KEY ("CardInstanceId") REFERENCES public."CardInstance"("Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_CardSetting_CardSettingId" FOREIGN KEY ("CardSettingId") REFERENCES public."CardSetting"("Id");


ALTER TABLE ONLY public."AcquiredCard"
    ADD CONSTRAINT "FK_AcquiredCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CardInstance"
    ADD CONSTRAINT "FK_CardInstance_Card_CardId" FOREIGN KEY ("CardId") REFERENCES public."Card"("Id");


ALTER TABLE ONLY public."CardInstance"
    ADD CONSTRAINT "FK_CardInstance_TemplateInstance_TemplateInstanceId" FOREIGN KEY ("TemplateInstanceId") REFERENCES public."TemplateInstance"("Id");


ALTER TABLE ONLY public."CardSetting"
    ADD CONSTRAINT "FK_CardSetting_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "FK_Card_CardInstance_CopySourceId" FOREIGN KEY ("CopySourceId") REFERENCES public."CardInstance"("Id");


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "FK_Card_Card_BranchSourceId" FOREIGN KEY ("BranchSourceId") REFERENCES public."Card"("Id");


ALTER TABLE ONLY public."Card"
    ADD CONSTRAINT "FK_Card_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance_CardInstance"
    ADD CONSTRAINT "FK_CommFieldInst_CardInst_CommFieldInst_CommFieldInstId" FOREIGN KEY ("CommunalFieldInstanceId") REFERENCES public."CommunalFieldInstance"("Id");


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "FK_CommentCard_Card_CardId" FOREIGN KEY ("CardId") REFERENCES public."Card"("Id");


ALTER TABLE ONLY public."CommentCard"
    ADD CONSTRAINT "FK_CommentCard_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommentTemplate"
    ADD CONSTRAINT "FK_CommentTemplate_Template_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."Template"("Id");


ALTER TABLE ONLY public."CommentTemplate"
    ADD CONSTRAINT "FK_CommentTemplate_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance_CardInstance"
    ADD CONSTRAINT "FK_CommunalFieldInst_CardInst_CardInst_CardInstId" FOREIGN KEY ("CardInstanceId") REFERENCES public."CardInstance"("Id");


ALTER TABLE ONLY public."CommunalFieldInstance"
    ADD CONSTRAINT "FK_CommunalFieldInstance_CommunalField_CommunalFieldId" FOREIGN KEY ("CommunalFieldId") REFERENCES public."CommunalField"("Id");


ALTER TABLE ONLY public."CommunalField"
    ADD CONSTRAINT "FK_CommunalField_User_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."Feedback"
    ADD CONSTRAINT "FK_Feedback_Feedback_ParentId" FOREIGN KEY ("ParentId") REFERENCES public."Feedback"("Id");


ALTER TABLE ONLY public."Feedback"
    ADD CONSTRAINT "FK_Feedback_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."File_CardInstance"
    ADD CONSTRAINT "FK_File_CardInstance_CardInstance_CardInstanceId" FOREIGN KEY ("CardInstanceId") REFERENCES public."CardInstance"("Id");


ALTER TABLE ONLY public."File_CardInstance"
    ADD CONSTRAINT "FK_File_CardInstance_File_FileId" FOREIGN KEY ("FileId") REFERENCES public."File"("Id");


ALTER TABLE ONLY public."Filter"
    ADD CONSTRAINT "FK_Filter_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User"("Id");


ALTER TABLE ONLY public."History"
    ADD CONSTRAINT "FK_History_AcquiredCard_AcquiredCardId" FOREIGN KEY ("AcquiredCardId") REFERENCES public."AcquiredCard"("Id") ON DELETE CASCADE;


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "FK_Relationship_AcquiredCard_AcquiredCard_SourceAcquiredCardId" FOREIGN KEY ("SourceAcquiredCardId") REFERENCES public."AcquiredCard"("Id");


ALTER TABLE ONLY public."Relationship_AcquiredCard"
    ADD CONSTRAINT "FK_Relationship_AcquiredCard_AcquiredCard_TargetAcquiredCardId" FOREIGN KEY ("TargetAcquiredCardId") REFERENCES public."AcquiredCard"("Id");


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



