USE [master]
GO
/****** Object:  Database [CardOverflow] ******/
CREATE DATABASE [CardOverflow]
GO
ALTER DATABASE [CardOverflow] SET COMPATIBILITY_LEVEL = 130
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [CardOverflow].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [CardOverflow] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [CardOverflow] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [CardOverflow] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [CardOverflow] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [CardOverflow] SET ARITHABORT OFF 
GO
ALTER DATABASE [CardOverflow] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [CardOverflow] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [CardOverflow] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [CardOverflow] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [CardOverflow] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [CardOverflow] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [CardOverflow] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [CardOverflow] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [CardOverflow] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [CardOverflow] SET  ENABLE_BROKER 
GO
ALTER DATABASE [CardOverflow] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [CardOverflow] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [CardOverflow] SET TRUSTWORTHY ON 
GO
ALTER DATABASE [CardOverflow] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [CardOverflow] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [CardOverflow] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [CardOverflow] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [CardOverflow] SET RECOVERY FULL 
GO
ALTER DATABASE [CardOverflow] SET  MULTI_USER 
GO
ALTER DATABASE [CardOverflow] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [CardOverflow] SET DB_CHAINING OFF 
GO
ALTER DATABASE [CardOverflow] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [CardOverflow] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [CardOverflow] SET DELAYED_DURABILITY = DISABLED 
GO
EXEC sys.sp_db_vardecimal_storage_format N'CardOverflow', N'ON'
GO
ALTER DATABASE [CardOverflow] SET QUERY_STORE = OFF
GO
USE [CardOverflow]
GO
ALTER DATABASE SCOPED CONFIGURATION SET LEGACY_CARDINALITY_ESTIMATION = OFF;
GO
ALTER DATABASE SCOPED CONFIGURATION FOR SECONDARY SET LEGACY_CARDINALITY_ESTIMATION = PRIMARY;
GO
ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 0;
GO
ALTER DATABASE SCOPED CONFIGURATION FOR SECONDARY SET MAXDOP = PRIMARY;
GO
ALTER DATABASE SCOPED CONFIGURATION SET PARAMETER_SNIFFING = ON;
GO
ALTER DATABASE SCOPED CONFIGURATION FOR SECONDARY SET PARAMETER_SNIFFING = PRIMARY;
GO
ALTER DATABASE SCOPED CONFIGURATION SET QUERY_OPTIMIZER_HOTFIXES = OFF;
GO
ALTER DATABASE SCOPED CONFIGURATION FOR SECONDARY SET QUERY_OPTIMIZER_HOTFIXES = PRIMARY;
GO
USE [CardOverflow]
GO
/****** Object:  FullTextCatalog [CardInstanceFieldValueFullTextCatalog] ******/
CREATE FULLTEXT CATALOG [CardInstanceFieldValueFullTextCatalog] WITH ACCENT_SENSITIVITY = ON
GO
/****** Object:  FullTextCatalog [RelationshipFullTextCatalog] ******/
CREATE FULLTEXT CATALOG [RelationshipFullTextCatalog] WITH ACCENT_SENSITIVITY = OFF
GO
/****** Object:  FullTextCatalog [TagFullTextCatalog] ******/
CREATE FULLTEXT CATALOG [TagFullTextCatalog] WITH ACCENT_SENSITIVITY = OFF
GO
/****** Object:  FullTextCatalog [TemplateFullTextCatalog] ******/
CREATE FULLTEXT CATALOG [TemplateFullTextCatalog] WITH ACCENT_SENSITIVITY = OFF
GO
/****** Object:  Table [dbo].[CardInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CardInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Created] [datetime2](7) NOT NULL,
	[Modified] [datetime2](7) NULL,
	[CardId] [int] NOT NULL,
	[IsDmca] [bit] NOT NULL,
	[FieldValues] [nvarchar](max) NOT NULL,
	[TemplateInstanceId] [int] NOT NULL,
	[Users] [int] NOT NULL,
	[EditSummary] [nvarchar](200) NOT NULL,
	[AnkiNoteId] [bigint] NULL,
	[AnkiNoteOrd] [tinyint] NULL,
	[Hash] [binary](64) NOT NULL,
 CONSTRAINT [PK_CardInstance] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AcquiredCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AcquiredCard](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[CardInstanceId] [int] NOT NULL,
	[CardState] [tinyint] NOT NULL,
	[EaseFactorInPermille] [smallint] NOT NULL,
	[IntervalOrStepsIndex] [smallint] NOT NULL,
	[Due] [smalldatetime] NOT NULL,
	[CardSettingId] [int] NOT NULL,
	[IsLapsed] [bit] NOT NULL,
 CONSTRAINT [PK_AcquiredCard] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[UserAndCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[UserAndCard] WITH SCHEMABINDING AS
SELECT
    ac.[UserId] UserId,
    ci.[CardId] CardId
FROM [dbo].[AcquiredCard] ac
JOIN [dbo].[CardInstance] ci ON ac.CardInstanceId = ci.Id
GO
SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF
GO
/****** Object:  Index [IX_UserAndCard_UserId_CardId] ******/
CREATE UNIQUE CLUSTERED INDEX [IX_UserAndCard_UserId_CardId] ON [dbo].[UserAndCard]
(
	[UserId] ASC,
	[CardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Card] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Card](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AuthorId] [int] NOT NULL,
	[Users] [int] NOT NULL,
 CONSTRAINT [PK_Card] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tag] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tag](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_Tag] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tag_AcquiredCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tag_AcquiredCard](
	[TagId] [int] NOT NULL,
	[AcquiredCardId] [int] NOT NULL,
 CONSTRAINT [PK_Tag_AcquiredCard] PRIMARY KEY CLUSTERED 
(
	[TagId] ASC,
	[AcquiredCardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[CardTagCount] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[CardTagCount]
AS
SELECT
    c.Id CardId,
    (SELECT TOP 1 t.Name FROM dbo.Tag t WHERE t.Id = ta.TagId) [Name],
    COUNT(*) [Count]
FROM dbo.Card c
JOIN dbo.CardInstance i ON c.Id = i.CardId
JOIN dbo.AcquiredCard ac ON ac.CardInstanceId = i.Id
JOIN dbo.Tag_AcquiredCard ta ON ta.AcquiredCardId = ac.Id
GROUP BY c.Id, ta.TagId
GO
/****** Object:  View [dbo].[CardInstanceTagCount] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[CardInstanceTagCount]
AS
SELECT
    i.Id CardInstanceId,
    (SELECT TOP 1 t.Name FROM dbo.Tag t WHERE t.Id = ta.TagId) [Name],
    COUNT(*) [Count]
FROM dbo.CardInstance i
JOIN dbo.AcquiredCard ac ON ac.CardInstanceId = i.Id
JOIN dbo.Tag_AcquiredCard ta ON ta.AcquiredCardId = ac.Id
GROUP BY i.Id, ta.TagId
GO
/****** Object:  View [dbo].[LatestCardInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW [dbo].[LatestCardInstance] AS
--https://stackoverflow.com/a/2111420
SELECT c.AuthorId
      ,c.Users as CardUsers
      ,i1.Id as CardInstanceId
	  ,i1.CardId
      ,i1.Created
      ,i1.Modified
      ,i1.IsDmca
      ,i1.FieldValues
      ,i1.TemplateInstanceId
      ,i1.Users as InstanceUsers
      ,i1.EditSummary
      ,i1.AnkiNoteId
      ,i1.AnkiNoteOrd
  FROM [Card] c
  JOIN [CardInstance] i1 on (c.Id = i1.CardId)
  LEFT OUTER JOIN [CardInstance] i2 ON (c.Id = i2.CardId AND 
    (i1.Created < i2.Created OR (i1.Created = i2.Created AND i1.id < i2.id)))
WHERE i2.id IS NULL;
GO
/****** Object:  View [dbo].[AcquiredCardIsLatest] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW [dbo].[AcquiredCardIsLatest] AS
SELECT a.*,
	CAST(
		CASE
			WHEN l.CardInstanceId IS NULL
			THEN 0
			ELSE 1
		END
	AS BIT) AS IsLatest
FROM [AcquiredCard] a
LEFT JOIN [LatestCardInstance] l on (l.CardInstanceId = a.CardInstanceId)
GO
/****** Object:  Table [dbo].[CommunalField] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommunalField](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AuthorId] [int] NOT NULL,
 CONSTRAINT [PK_CommunalField] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommunalFieldInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommunalFieldInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CommunalFieldId] [int] NOT NULL,
	[FieldName] [nvarchar](200) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
	[Created] [datetime2](7) NOT NULL,
	[Modified] [datetime2](7) NULL,
	[EditSummary] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_CommunalFieldInstance] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  View [dbo].[LatestCommunalFieldInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW [dbo].[LatestCommunalFieldInstance] AS
--https://stackoverflow.com/a/2111420
SELECT c.AuthorId
      ,i1.Id as CommunalFieldInstanceId
      ,i1.CommunalFieldId
      ,i1.FieldName
      ,i1.Value
      ,i1.Created
      ,i1.Modified
      ,i1.EditSummary
  FROM [CommunalField] c
  JOIN [CommunalFieldInstance] i1 on (c.Id = i1.CommunalFieldId)
  LEFT OUTER JOIN [CommunalFieldInstance] i2 ON (c.Id = i2.CommunalFieldId AND 
    (i1.Created < i2.Created OR (i1.Created = i2.Created AND i1.id < i2.id)))
WHERE i2.id IS NULL;
GO
/****** Object:  Table [dbo].[Template] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Template](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AuthorId] [int] NOT NULL,
 CONSTRAINT [PK_Template] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TemplateInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[TemplateId] [int] NOT NULL,
	[Css] [varchar](4000) NOT NULL,
	[Created] [datetime2](7) NOT NULL,
	[Modified] [datetime2](7) NULL,
	[LatexPre] [nvarchar](500) NOT NULL,
	[LatexPost] [nvarchar](500) NOT NULL,
	[IsDmca] [bit] NOT NULL,
	[QuestionTemplate] [nvarchar](4000) NOT NULL,
	[AnswerTemplate] [nvarchar](4000) NOT NULL,
	[ShortQuestionTemplate] [nvarchar](200) NOT NULL,
	[ShortAnswerTemplate] [nvarchar](200) NOT NULL,
	[Fields] [nvarchar](4000) NOT NULL,
	[EditSummary] [nvarchar](200) NOT NULL,
	[AnkiId] [bigint] NULL,
	[Hash] [binary](64) NOT NULL,
 CONSTRAINT [PK_TemplateInstance] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[LatestTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW [dbo].[LatestTemplateInstance] AS
--https://stackoverflow.com/a/2111420
SELECT c.AuthorId
      ,i1.Id as TemplateInstanceId
      ,i1.TemplateId
      ,i1.Name
      ,i1.Css
      ,i1.Created
      ,i1.Modified
      ,i1.LatexPre
      ,i1.LatexPost
      ,i1.IsDmca
      ,i1.QuestionTemplate
      ,i1.AnswerTemplate
      ,i1.ShortQuestionTemplate
      ,i1.ShortAnswerTemplate
      ,i1.Fields
      ,i1.EditSummary
      ,i1.AnkiId
  FROM dbo.[Template] c
  JOIN [TemplateInstance] i1 on (c.Id = i1.TemplateId)
  LEFT OUTER JOIN [TemplateInstance] i2 ON (c.Id = i2.TemplateId AND 
    (i1.Created < i2.Created OR (i1.Created = i2.Created AND i1.id < i2.id)))
WHERE i2.id IS NULL;
GO
/****** Object:  Table [dbo].[Relationship] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Relationship](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_Relationship] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Relationship_AcquiredCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Relationship_AcquiredCard](
	[SourceAcquiredCardId] [int] NOT NULL,
	[TargetAcquiredCardId] [int] NOT NULL,
	[RelationshipId] [int] NOT NULL,
	CONSTRAINT source_not_equal_target CHECK(SourceAcquiredCardId <> TargetAcquiredCardId),
 CONSTRAINT [PK_Relationship_AcquiredCard] PRIMARY KEY CLUSTERED 
(
	[SourceAcquiredCardId] ASC,
	[TargetAcquiredCardId] ASC,
	[RelationshipId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[CardRelationshipCount] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[CardRelationshipCount]
AS
SELECT * FROM (
    SELECT
        si.CardId SourceCardId,
        ti.CardId TargetCardId,
        (SELECT TOP 1 r.Name
        FROM dbo.Relationship r
        WHERE r.Id = rac.RelationshipId) [Name],
        Count(*) [Count]
    FROM dbo.Relationship_AcquiredCard rac
    JOIN dbo.AcquiredCard sac ON rac.SourceAcquiredCardId = sac.Id
    JOIN dbo.AcquiredCard tac ON rac.TargetAcquiredCardId = tac.Id
    JOIN dbo.CardInstance si ON sac.CardInstanceId = si.Id
    JOIN dbo.CardInstance ti ON tac.CardInstanceId = ti.Id
    GROUP BY si.CardId, ti.CardId, rac.RelationshipId
    ) X
CROSS APPLY -- https://dba.stackexchange.com/q/259798
    (values (X.SourceCardId),
            (X.TargetCardId)
    ) _ (CardId)

GO
/****** Object:  View [dbo].[CardInstanceRelationshipCount] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[CardInstanceRelationshipCount]
AS
SELECT * FROM (
    SELECT
        sac.CardInstanceId SourceCardInstanceId,
        tac.CardInstanceId TargetCardInstanceId,
        (SELECT TOP 1 r.Name
        FROM dbo.Relationship r
        WHERE r.Id = rac.RelationshipId) [Name],
        Count(*) [Count]
    FROM dbo.Relationship_AcquiredCard rac
    JOIN dbo.AcquiredCard sac ON rac.SourceAcquiredCardId = sac.Id
    JOIN dbo.AcquiredCard tac ON rac.TargetAcquiredCardId = tac.Id
    GROUP BY sac.CardInstanceId, tac.CardInstanceId, rac.RelationshipId
    ) X
CROSS APPLY -- https://dba.stackexchange.com/q/259798
    (values (X.SourceCardInstanceId),
            (X.TargetCardInstanceId)
    ) _ (CardInstanceId)
GO
/****** Object:  Table [dbo].[AlphaBetaKey] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AlphaBetaKey](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Key] [nvarchar](50) NOT NULL,
	[IsUsed] [bit] NOT NULL,
 CONSTRAINT [PK_AlphaBetaKey] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetRoleClaims] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [int] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetRoles] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserClaims] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserLogins] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](450) NOT NULL,
	[ProviderKey] [nvarchar](450) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserRoles] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [int] NOT NULL,
	[RoleId] [int] NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserTokens] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserTokens](
	[UserId] [int] NOT NULL,
	[LoginProvider] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CardSetting] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CardSetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[NewCardsStepsInMinutes] [varchar](100) NOT NULL,
	[NewCardsMaxPerDay] [smallint] NOT NULL,
	[NewCardsGraduatingIntervalInDays] [tinyint] NOT NULL,
	[NewCardsEasyIntervalInDays] [tinyint] NOT NULL,
	[NewCardsStartingEaseFactorInPermille] [smallint] NOT NULL,
	[NewCardsBuryRelated] [bit] NOT NULL,
	[MatureCardsMaxPerDay] [smallint] NOT NULL,
	[MatureCardsEaseFactorEasyBonusFactorInPermille] [smallint] NOT NULL,
	[MatureCardsIntervalFactorInPermille] [smallint] NOT NULL,
	[MatureCardsMaximumIntervalInDays] [smallint] NOT NULL,
	[MatureCardsHardIntervalFactorInPermille] [smallint] NOT NULL,
	[MatureCardsBuryRelated] [bit] NOT NULL,
	[LapsedCardsStepsInMinutes] [varchar](100) NOT NULL,
	[LapsedCardsNewIntervalFactorInPermille] [smallint] NOT NULL,
	[LapsedCardsMinimumIntervalInDays] [tinyint] NOT NULL,
	[LapsedCardsLeechThreshold] [tinyint] NOT NULL,
	[ShowAnswerTimer] [bit] NOT NULL,
	[AutomaticallyPlayAudio] [bit] NOT NULL,
	[ReplayQuestionAudioOnAnswer] [bit] NOT NULL,
 CONSTRAINT [PK_CardSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommentCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommentCard](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CardId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_CommentCard] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommentTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommentTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_CommentTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommunalFieldInstance_CardInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommunalFieldInstance_CardInstance](
	[CardInstanceId] [int] NOT NULL,
	[CommunalFieldInstanceId] [int] NOT NULL,
 CONSTRAINT [PK_CommunalFieldInstance_CardInstance] PRIMARY KEY CLUSTERED 
(
	[CommunalFieldInstanceId] ASC,
	[CardInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Feedback] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Feedback](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](1000) NOT NULL,
	[UserId] [int] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[ParentId] [int] NULL,
	[Priority] [tinyint] NULL,
 CONSTRAINT [PK_Feedback] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[File] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[File](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FileName] [nvarchar](200) NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[Sha256] [binary](32) NOT NULL,
 CONSTRAINT [PK_File] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[File_CardInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[File_CardInstance](
	[CardInstanceId] [int] NOT NULL,
	[FileId] [int] NOT NULL,
 CONSTRAINT [PK_File_CardInstance] PRIMARY KEY CLUSTERED 
(
	[CardInstanceId] ASC,
	[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Filter] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Filter](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[UserId] [int] NOT NULL,
	[Query] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_Filter] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[History] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[History](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AcquiredCardId] [int] NOT NULL,
	[Score] [tinyint] NOT NULL,
	[Timestamp] [smalldatetime] NOT NULL,
	[IntervalWithUnusedStepsIndex] [smallint] NOT NULL,
	[EaseFactorInPermille] [smallint] NOT NULL,
	[TimeFromSeeingQuestionToScoreInSecondsPlus32768] [smallint] NOT NULL,
 CONSTRAINT [PK_History] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PotentialSignups] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PotentialSignups](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Email] [nvarchar](500) NOT NULL,
	[Message] [nvarchar](1000) NOT NULL,
	[OneIsAlpha2Beta3Ga] [tinyint] NOT NULL,
	[TimeStamp] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_PotentialSignups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tag_User_TemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tag_User_TemplateInstance](
	[UserId] [int] NOT NULL,
	[TemplateInstanceId] [int] NOT NULL,
	[DefaultTagId] [int] NOT NULL,
 CONSTRAINT [PK_Tag_User_TemplateInstance] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[TemplateInstanceId] ASC,
	[DefaultTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
	[DisplayName] [nvarchar](32) NULL,
	[DefaultCardSettingId] [int] NULL,
	[ShowNextReviewTime] [bit] NOT NULL,
	[ShowRemainingCardCount] [bit] NOT NULL,
	[MixNewAndReview] [tinyint] NOT NULL,
	[NextDayStartsAtXHoursPastMidnight] [tinyint] NOT NULL,
	[LearnAheadLimitInMinutes] [tinyint] NOT NULL,
	[TimeboxTimeLimitInMinutes] [tinyint] NOT NULL,
	[IsNightMode] [bit] NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User_TemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User_TemplateInstance](
	[UserId] [int] NOT NULL,
	[TemplateInstanceId] [int] NOT NULL,
	[DefaultCardSettingId] [int] NOT NULL,
 CONSTRAINT [PK_User_TemplateInstance] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[TemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_CommentCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_CommentCard](
	[CommentCardId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_CommentCard] PRIMARY KEY CLUSTERED 
(
	[CommentCardId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_CommentTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_CommentTemplate](
	[CommentTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_CommentTemplate] PRIMARY KEY CLUSTERED 
(
	[CommentTemplateId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_Feedback] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_Feedback](
	[FeedbackId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_Feedback] PRIMARY KEY CLUSTERED 
(
	[FeedbackId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[CardSetting] ON 

INSERT [dbo].[CardSetting] ([Id], [UserId], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (1, 1, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
INSERT [dbo].[CardSetting] ([Id], [UserId], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (2, 2, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
INSERT [dbo].[CardSetting] ([Id], [UserId], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (3, 3, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
SET IDENTITY_INSERT [dbo].[CardSetting] OFF
SET IDENTITY_INSERT [dbo].[Template] ON 

INSERT [dbo].[Template] ([Id], [AuthorId]) VALUES (1, 2)
INSERT [dbo].[Template] ([Id], [AuthorId]) VALUES (2, 2)
INSERT [dbo].[Template] ([Id], [AuthorId]) VALUES (3, 2)
INSERT [dbo].[Template] ([Id], [AuthorId]) VALUES (4, 2)
INSERT [dbo].[Template] ([Id], [AuthorId]) VALUES (5, 2)
SET IDENTITY_INSERT [dbo].[Template] OFF
SET IDENTITY_INSERT [dbo].[TemplateInstance] ON 

INSERT [dbo].[TemplateInstance] ([Id], [Name], [TemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [Fields], [EditSummary], [AnkiId], [Hash]) VALUES (1, N'Basic', 1, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:29.5810000' AS DateTime2), CAST(N'2019-06-16T00:53:30.0000000' AS DateTime2), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0, N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', N'FrontArial20False0FalseBackArial20False1False', N'Imported from Anki', 1554689669581, 0xCB0A06105B2CBE2E2DDE79F01A88B336D6F89A3B7C5E23753EF92FC05BEEFEFEFCE69B1D89D40B286DE537F6823F1C18B36F7F4F17912518ECEBDA9AED89ACBE)
INSERT [dbo].[TemplateInstance] ([Id], [Name], [TemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [Fields], [EditSummary], [AnkiId], [Hash]) VALUES (2, N'Basic (and reversed card) - Card 1', 2, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:29.5770000' AS DateTime2), CAST(N'2019-06-16T00:51:28.0000000' AS DateTime2), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0, N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', N'FrontArial20False0FalseBackArial20False1False', N'Imported from Anki', 1554689669577, 0x7865EC597180B1001F1E050693824B210DEE685EBCEF3AEC1D80FF3E83E7D9DCE6A586F6BCC771AADD48CE7903DF9D5FAC673D1BFD5ABD0FA5A09D44CDE48FBB)
INSERT [dbo].[TemplateInstance] ([Id], [Name], [TemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [Fields], [EditSummary], [AnkiId], [Hash]) VALUES (3, N'Basic (optional reversed card) - Card 1', 3, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:29.5720000' AS DateTime2), CAST(N'2019-06-16T00:51:32.0000000' AS DateTime2), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0, N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', N'FrontArial20False0FalseBackArial20False1FalseAdd ReverseArial20False2False', N'Imported from Anki', 1554689669572, 0xBE066200F645231B9AA47D4DF0E803F99E1197765DCAACDA45EE5E0029F2813F0440388B4E99EA72A32BB0BA16D8F85E40C61499AECD65A5D5CC0E285A916A8F)
INSERT [dbo].[TemplateInstance] ([Id], [Name], [TemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [Fields], [EditSummary], [AnkiId], [Hash]) VALUES (4, N'Basic (type in the answer)', 4, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:29.5710000' AS DateTime2), CAST(N'2019-06-16T00:51:46.0000000' AS DateTime2), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0, N'{{Front}}
{{type:Back}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', N'FrontArial20False0FalseBackArial20False1False', N'Imported from Anki', 1554689669571, 0x1DC35754E5575169D3E1A3FF8013BE0ADB80EB1DA7BD749100F0927985E84B19BC81A7B89F232D55B17E2DC0F8CE1DEC8A487C77AF9DAF4B2D2BF36324453326)
INSERT [dbo].[TemplateInstance] ([Id], [Name], [TemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [Fields], [EditSummary], [AnkiId], [Hash]) VALUES (5, N'Cloze', 5, N'.card {
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
}', CAST(N'2019-04-08T02:14:29.5700000' AS DateTime2), CAST(N'2019-06-16T00:51:55.0000000' AS DateTime2), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0, N'{{cloze:Text}}', N'{{cloze:Text}}<br>
{{Extra}}', N'', N'', N'TextArial20False0FalseExtraArial20False1False', N'Imported from Anki', 1554689669570, 0x3C43C6FE47B095D4E2E12D8DB1B91110C72090ADB383FD04FC6D6C868A591E6840D47F74E301035333A55A7A16DC6E5DA5E9E4FCAE81C4E64E66C34A03AFAAC2)
SET IDENTITY_INSERT [dbo].[TemplateInstance] OFF
SET IDENTITY_INSERT [dbo].[User] ON 

INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName], [DefaultCardSettingId], [ShowNextReviewTime], [ShowRemainingCardCount], [MixNewAndReview], [NextDayStartsAtXHoursPastMidnight], [LearnAheadLimitInMinutes], [TimeboxTimeLimitInMinutes], [IsNightMode]) VALUES (1, NULL, NULL, N'admin@cardoverflow.io', NULL, 0, NULL, NULL, N'4934a9df-035b-4216-a8d7-cf00510a16ff', NULL, 0, 0, NULL, 0, 0, N'Admin', 1, 1, 1, 0, 4, 20, 0, 0)
INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName], [DefaultCardSettingId], [ShowNextReviewTime], [ShowRemainingCardCount], [MixNewAndReview], [NextDayStartsAtXHoursPastMidnight], [LearnAheadLimitInMinutes], [TimeboxTimeLimitInMinutes], [IsNightMode]) VALUES (2, NULL, NULL, N'theCollective@cardoverflow.io', NULL, 0, NULL, NULL, N'7f15011b-1605-4b2c-ba98-af5659739d60', NULL, 0, 0, NULL, 0, 0, N'The Collective', 2, 1, 1, 0, 4, 20, 0, 0)
INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName], [DefaultCardSettingId], [ShowNextReviewTime], [ShowRemainingCardCount], [MixNewAndReview], [NextDayStartsAtXHoursPastMidnight], [LearnAheadLimitInMinutes], [TimeboxTimeLimitInMinutes], [IsNightMode]) VALUES (3, NULL, NULL, N'roboturtle@cardoverflow.io', NULL, 0, NULL, NULL, N'd622b1ce-0c3b-48a3-9851-506e17bd04ec', NULL, 0, 0, NULL, 0, 0, N'RoboTurtle', 3, 1, 1, 0, 4, 20, 0, 0)
SET IDENTITY_INSERT [dbo].[User] OFF
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (1, 1, 1)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (1, 2, 1)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (1, 3, 1)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (1, 4, 1)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (1, 5, 1)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (2, 1, 2)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (2, 2, 2)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (2, 3, 2)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (2, 4, 2)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (2, 5, 2)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (3, 1, 3)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (3, 2, 3)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (3, 3, 3)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (3, 4, 3)
INSERT [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId], [DefaultCardSettingId]) VALUES (3, 5, 3)
/****** Object:  Index [IX_AcquiredCard_CardInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_CardInstanceId] ON [dbo].[AcquiredCard]
(
	[CardInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AcquiredCard_CardSettingId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_CardSettingId] ON [dbo].[AcquiredCard]
(
	[CardSettingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AcquiredCard_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_UserId] ON [dbo].[AcquiredCard]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AcquiredCard_UserId_CardInstanceId] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_AcquiredCard_UserId_CardInstanceId] ON [dbo].[AcquiredCard]
(
	[UserId] ASC,
	[CardInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AlphaBetaKey_Key] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_AlphaBetaKey_Key] ON [dbo].[AlphaBetaKey]
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetRoleClaims_RoleId] ******/
CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [RoleNameIndex] ******/
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]
(
	[NormalizedName] ASC
)
WHERE ([NormalizedName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetUserClaims_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetUserLogins_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetUserRoles_RoleId] ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Card_AuthorId] ******/
CREATE NONCLUSTERED INDEX [IX_Card_AuthorId] ON [dbo].[Card]
(
	[AuthorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CardInstance_CardId] ******/
CREATE NONCLUSTERED INDEX [IX_CardInstance_CardId] ON [dbo].[CardInstance]
(
	[CardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_CardInstance_Hash] ******/
CREATE NONCLUSTERED INDEX [IX_CardInstance_Hash] ON [dbo].[CardInstance]
(
	[Hash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CardInstance_TemplateInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_CardInstance_TemplateInstanceId] ON [dbo].[CardInstance]
(
	[TemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CardSetting_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_CardSetting_UserId] ON [dbo].[CardSetting]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentCard_CardId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentCard_CardId] ON [dbo].[CommentCard]
(
	[CardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentCard_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentCard_UserId] ON [dbo].[CommentCard]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentTemplate_TemplateId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentTemplate_TemplateId] ON [dbo].[CommentTemplate]
(
	[TemplateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentTemplate_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentTemplate_UserId] ON [dbo].[CommentTemplate]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommunalField_AuthorId] ******/
CREATE NONCLUSTERED INDEX [IX_CommunalField_AuthorId] ON [dbo].[CommunalField]
(
	[AuthorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommunalFieldInstance_CommunalFieldId] ******/
CREATE NONCLUSTERED INDEX [IX_CommunalFieldInstance_CommunalFieldId] ON [dbo].[CommunalFieldInstance]
(
	[CommunalFieldId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommunalFieldInstance_CardInstance_CardInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_CommunalFieldInstance_CardInstance_CardInstanceId] ON [dbo].[CommunalFieldInstance_CardInstance]
(
	[CardInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Feedback_ParentId] ******/
CREATE NONCLUSTERED INDEX [IX_Feedback_ParentId] ON [dbo].[Feedback]
(
	[ParentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Feedback_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Feedback_UserId] ON [dbo].[Feedback]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_File_Sha256] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_File_Sha256] ON [dbo].[File]
(
	[Sha256] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_File_CardInstance_FileId] ******/
CREATE NONCLUSTERED INDEX [IX_File_CardInstance_FileId] ON [dbo].[File_CardInstance]
(
	[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Filter_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Filter_UserId] ON [dbo].[Filter]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_History_AcquiredCardId] ******/
CREATE NONCLUSTERED INDEX [IX_History_AcquiredCardId] ON [dbo].[History]
(
	[AcquiredCardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Relationship_Name] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Relationship_Name] ON [dbo].[Relationship]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Relationship_AcquiredCard_RelationshipId] ******/
CREATE NONCLUSTERED INDEX [IX_Relationship_AcquiredCard_RelationshipId] ON [dbo].[Relationship_AcquiredCard]
(
	[RelationshipId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Relationship_AcquiredCard_TargetAcquiredCardId] ******/
CREATE NONCLUSTERED INDEX [IX_Relationship_AcquiredCard_TargetAcquiredCardId] ON [dbo].[Relationship_AcquiredCard]
(
	[TargetAcquiredCardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Tag_Name] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tag_Name] ON [dbo].[Tag]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Tag_AcquiredCard_AcquiredCardId] ******/
CREATE NONCLUSTERED INDEX [IX_Tag_AcquiredCard_AcquiredCardId] ON [dbo].[Tag_AcquiredCard]
(
	[AcquiredCardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Tag_User_TemplateInstance_DefaultTagId] ******/
CREATE NONCLUSTERED INDEX [IX_Tag_User_TemplateInstance_DefaultTagId] ON [dbo].[Tag_User_TemplateInstance]
(
	[DefaultTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Template_AuthorId] ******/
CREATE NONCLUSTERED INDEX [IX_Template_AuthorId] ON [dbo].[Template]
(
	[AuthorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_TemplateInstance_Hash] ******/
CREATE NONCLUSTERED INDEX [IX_TemplateInstance_Hash] ON [dbo].[TemplateInstance]
(
	[Hash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_TemplateInstance_TemplateId] ******/
CREATE NONCLUSTERED INDEX [IX_TemplateInstance_TemplateId] ON [dbo].[TemplateInstance]
(
	[TemplateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [EmailIndex] ******/
CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[User]
(
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_User_DisplayName] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_User_DisplayName] ON [dbo].[User]
(
	[DisplayName] ASC
)
WHERE ([DisplayName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_User_Email] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_User_Email] ON [dbo].[User]
(
	[Email] ASC
)
WHERE ([Email] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UserNameIndex] ******/
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[User]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_User_TemplateInstance_DefaultCardSettingId] ******/
CREATE NONCLUSTERED INDEX [IX_User_TemplateInstance_DefaultCardSettingId] ON [dbo].[User_TemplateInstance]
(
	[DefaultCardSettingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_User_TemplateInstance_TemplateInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_User_TemplateInstance_TemplateInstanceId] ON [dbo].[User_TemplateInstance]
(
	[TemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_CommentCard_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_CommentCard_UserId] ON [dbo].[Vote_CommentCard]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_CommentTemplate_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_CommentTemplate_UserId] ON [dbo].[Vote_CommentTemplate]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_Feedback_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_Feedback_UserId] ON [dbo].[Vote_Feedback]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  FullTextIndex ******/
CREATE FULLTEXT INDEX ON [dbo].[CardInstance](
[FieldValues] LANGUAGE 'English')
KEY INDEX [PK_CardInstance]ON ([CardInstanceFieldValueFullTextCatalog], FILEGROUP [PRIMARY])
WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)

GO
/****** Object:  FullTextIndex ******/
CREATE FULLTEXT INDEX ON [dbo].[Relationship](
[Name] LANGUAGE 'English')
KEY INDEX [PK_Relationship]ON ([RelationshipFullTextCatalog], FILEGROUP [PRIMARY])
WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)

GO
/****** Object:  FullTextIndex ******/
CREATE FULLTEXT INDEX ON [dbo].[Tag](
[Name] LANGUAGE 'English')
KEY INDEX [PK_Tag]ON ([TagFullTextCatalog], FILEGROUP [PRIMARY])
WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)

GO
/****** Object:  FullTextIndex ******/
CREATE FULLTEXT INDEX ON [dbo].[TemplateInstance](
[AnswerTemplate] LANGUAGE 'English', 
[Css] LANGUAGE 'English', 
[Fields] LANGUAGE 'English', 
[LatexPost] LANGUAGE 'English', 
[LatexPre] LANGUAGE 'English', 
[Name] LANGUAGE 'English', 
[QuestionTemplate] LANGUAGE 'English', 
[ShortAnswerTemplate] LANGUAGE 'English', 
[ShortQuestionTemplate] LANGUAGE 'English')
KEY INDEX [PK_TemplateInstance]ON ([TemplateFullTextCatalog], FILEGROUP [PRIMARY])
WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)

GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_CardInstance_CardInstanceId] FOREIGN KEY([CardInstanceId])
REFERENCES [dbo].[CardInstance] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_CardInstance_CardInstanceId]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_CardSetting_CardSettingId] FOREIGN KEY([CardSettingId])
REFERENCES [dbo].[CardSetting] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_CardSetting_CardSettingId]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_User_UserId]
GO
ALTER TABLE [dbo].[AspNetRoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetRoleClaims] CHECK CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserClaims_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserClaims] CHECK CONSTRAINT [FK_AspNetUserClaims_User_UserId]
GO
ALTER TABLE [dbo].[AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserLogins_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserLogins] CHECK CONSTRAINT [FK_AspNetUserLogins_User_UserId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_User_UserId]
GO
ALTER TABLE [dbo].[AspNetUserTokens]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserTokens_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserTokens] CHECK CONSTRAINT [FK_AspNetUserTokens_User_UserId]
GO
ALTER TABLE [dbo].[Card]  WITH CHECK ADD  CONSTRAINT [FK_Card_User_AuthorId] FOREIGN KEY([AuthorId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Card] CHECK CONSTRAINT [FK_Card_User_AuthorId]
GO
ALTER TABLE [dbo].[CardInstance]  WITH CHECK ADD  CONSTRAINT [FK_CardInstance_Card_CardId] FOREIGN KEY([CardId])
REFERENCES [dbo].[Card] ([Id])
GO
ALTER TABLE [dbo].[CardInstance] CHECK CONSTRAINT [FK_CardInstance_Card_CardId]
GO
ALTER TABLE [dbo].[CardInstance]  WITH CHECK ADD  CONSTRAINT [FK_CardInstance_TemplateInstance_TemplateInstanceId] FOREIGN KEY([TemplateInstanceId])
REFERENCES [dbo].[TemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[CardInstance] CHECK CONSTRAINT [FK_CardInstance_TemplateInstance_TemplateInstanceId]
GO
ALTER TABLE [dbo].[CardSetting]  WITH CHECK ADD  CONSTRAINT [FK_CardSetting_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CardSetting] CHECK CONSTRAINT [FK_CardSetting_User_UserId]
GO
ALTER TABLE [dbo].[CommentCard]  WITH CHECK ADD  CONSTRAINT [FK_CommentCard_Card_CardId] FOREIGN KEY([CardId])
REFERENCES [dbo].[Card] ([Id])
GO
ALTER TABLE [dbo].[CommentCard] CHECK CONSTRAINT [FK_CommentCard_Card_CardId]
GO
ALTER TABLE [dbo].[CommentCard]  WITH CHECK ADD  CONSTRAINT [FK_CommentCard_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CommentCard] CHECK CONSTRAINT [FK_CommentCard_User_UserId]
GO
ALTER TABLE [dbo].[CommentTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CommentTemplate_Template_TemplateId] FOREIGN KEY([TemplateId])
REFERENCES [dbo].[Template] ([Id])
GO
ALTER TABLE [dbo].[CommentTemplate] CHECK CONSTRAINT [FK_CommentTemplate_Template_TemplateId]
GO
ALTER TABLE [dbo].[CommentTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CommentTemplate_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CommentTemplate] CHECK CONSTRAINT [FK_CommentTemplate_User_UserId]
GO
ALTER TABLE [dbo].[CommunalField]  WITH CHECK ADD  CONSTRAINT [FK_CommunalField_User_AuthorId] FOREIGN KEY([AuthorId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CommunalField] CHECK CONSTRAINT [FK_CommunalField_User_AuthorId]
GO
ALTER TABLE [dbo].[CommunalFieldInstance]  WITH CHECK ADD  CONSTRAINT [FK_CommunalFieldInstance_CommunalField_CommunalFieldId] FOREIGN KEY([CommunalFieldId])
REFERENCES [dbo].[CommunalField] ([Id])
GO
ALTER TABLE [dbo].[CommunalFieldInstance] CHECK CONSTRAINT [FK_CommunalFieldInstance_CommunalField_CommunalFieldId]
GO
ALTER TABLE [dbo].[CommunalFieldInstance_CardInstance]  WITH CHECK ADD  CONSTRAINT [FK_CommunalFieldInstance_CardInstance_CardInstance_CardInstanceId] FOREIGN KEY([CardInstanceId])
REFERENCES [dbo].[CardInstance] ([Id])
GO
ALTER TABLE [dbo].[CommunalFieldInstance_CardInstance] CHECK CONSTRAINT [FK_CommunalFieldInstance_CardInstance_CardInstance_CardInstanceId]
GO
ALTER TABLE [dbo].[CommunalFieldInstance_CardInstance]  WITH CHECK ADD  CONSTRAINT [FK_CommunalFieldInstance_CardInstance_CommunalFieldInstance_CommunalFieldInstanceId] FOREIGN KEY([CommunalFieldInstanceId])
REFERENCES [dbo].[CommunalFieldInstance] ([Id])
GO
ALTER TABLE [dbo].[CommunalFieldInstance_CardInstance] CHECK CONSTRAINT [FK_CommunalFieldInstance_CardInstance_CommunalFieldInstance_CommunalFieldInstanceId]
GO
ALTER TABLE [dbo].[Feedback]  WITH CHECK ADD  CONSTRAINT [FK_Feedback_Feedback_ParentId] FOREIGN KEY([ParentId])
REFERENCES [dbo].[Feedback] ([Id])
GO
ALTER TABLE [dbo].[Feedback] CHECK CONSTRAINT [FK_Feedback_Feedback_ParentId]
GO
ALTER TABLE [dbo].[Feedback]  WITH CHECK ADD  CONSTRAINT [FK_Feedback_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Feedback] CHECK CONSTRAINT [FK_Feedback_User_UserId]
GO
ALTER TABLE [dbo].[File_CardInstance]  WITH CHECK ADD  CONSTRAINT [FK_File_CardInstance_CardInstance_CardInstanceId] FOREIGN KEY([CardInstanceId])
REFERENCES [dbo].[CardInstance] ([Id])
GO
ALTER TABLE [dbo].[File_CardInstance] CHECK CONSTRAINT [FK_File_CardInstance_CardInstance_CardInstanceId]
GO
ALTER TABLE [dbo].[File_CardInstance]  WITH CHECK ADD  CONSTRAINT [FK_File_CardInstance_File_FileId] FOREIGN KEY([FileId])
REFERENCES [dbo].[File] ([Id])
GO
ALTER TABLE [dbo].[File_CardInstance] CHECK CONSTRAINT [FK_File_CardInstance_File_FileId]
GO
ALTER TABLE [dbo].[Filter]  WITH CHECK ADD  CONSTRAINT [FK_Filter_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Filter] CHECK CONSTRAINT [FK_Filter_User_UserId]
GO
ALTER TABLE [dbo].[History]  WITH CHECK ADD  CONSTRAINT [FK_History_AcquiredCard_AcquiredCardId] FOREIGN KEY([AcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[History] CHECK CONSTRAINT [FK_History_AcquiredCard_AcquiredCardId]
GO
ALTER TABLE [dbo].[Relationship_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_Relationship_AcquiredCard_AcquiredCard_SourceAcquiredCardId] FOREIGN KEY([SourceAcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
GO
ALTER TABLE [dbo].[Relationship_AcquiredCard] CHECK CONSTRAINT [FK_Relationship_AcquiredCard_AcquiredCard_SourceAcquiredCardId]
GO
ALTER TABLE [dbo].[Relationship_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_Relationship_AcquiredCard_AcquiredCard_TargetAcquiredCardId] FOREIGN KEY([TargetAcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
GO
ALTER TABLE [dbo].[Relationship_AcquiredCard] CHECK CONSTRAINT [FK_Relationship_AcquiredCard_AcquiredCard_TargetAcquiredCardId]
GO
ALTER TABLE [dbo].[Relationship_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_Relationship_AcquiredCard_Relationship_RelationshipId] FOREIGN KEY([RelationshipId])
REFERENCES [dbo].[Relationship] ([Id])
GO
ALTER TABLE [dbo].[Relationship_AcquiredCard] CHECK CONSTRAINT [FK_Relationship_AcquiredCard_Relationship_RelationshipId]
GO
ALTER TABLE [dbo].[Tag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_Tag_AcquiredCard_AcquiredCard_AcquiredCardId] FOREIGN KEY([AcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Tag_AcquiredCard] CHECK CONSTRAINT [FK_Tag_AcquiredCard_AcquiredCard_AcquiredCardId]
GO
ALTER TABLE [dbo].[Tag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_Tag_AcquiredCard_Tag_TagId] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tag] ([Id])
GO
ALTER TABLE [dbo].[Tag_AcquiredCard] CHECK CONSTRAINT [FK_Tag_AcquiredCard_Tag_TagId]
GO
ALTER TABLE [dbo].[Tag_User_TemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_Tag_User_TemplateInstance_Tag_DefaultTagId] FOREIGN KEY([DefaultTagId])
REFERENCES [dbo].[Tag] ([Id])
GO
ALTER TABLE [dbo].[Tag_User_TemplateInstance] CHECK CONSTRAINT [FK_Tag_User_TemplateInstance_Tag_DefaultTagId]
GO
ALTER TABLE [dbo].[Tag_User_TemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_Tag_User_TemplateInstance_User_TemplateInstance_UserId_TemplateInstanceId] FOREIGN KEY([UserId], [TemplateInstanceId])
REFERENCES [dbo].[User_TemplateInstance] ([UserId], [TemplateInstanceId])
GO
ALTER TABLE [dbo].[Tag_User_TemplateInstance] CHECK CONSTRAINT [FK_Tag_User_TemplateInstance_User_TemplateInstance_UserId_TemplateInstanceId]
GO
ALTER TABLE [dbo].[Template]  WITH CHECK ADD  CONSTRAINT [FK_Template_User_AuthorId] FOREIGN KEY([AuthorId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Template] CHECK CONSTRAINT [FK_Template_User_AuthorId]
GO
ALTER TABLE [dbo].[TemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_TemplateInstance_Template_TemplateId] FOREIGN KEY([TemplateId])
REFERENCES [dbo].[Template] ([Id])
GO
ALTER TABLE [dbo].[TemplateInstance] CHECK CONSTRAINT [FK_TemplateInstance_Template_TemplateId]
GO
ALTER TABLE [dbo].[User]  WITH CHECK ADD  CONSTRAINT [FK_User_CardSetting_DefaultCardSettingId] FOREIGN KEY([DefaultCardSettingId])
REFERENCES [dbo].[CardSetting] ([Id])
GO
ALTER TABLE [dbo].[User] CHECK CONSTRAINT [FK_User_CardSetting_DefaultCardSettingId]
GO
ALTER TABLE [dbo].[User_TemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_TemplateInstance_CardSetting_DefaultCardSettingId] FOREIGN KEY([DefaultCardSettingId])
REFERENCES [dbo].[CardSetting] ([Id])
GO
ALTER TABLE [dbo].[User_TemplateInstance] CHECK CONSTRAINT [FK_User_TemplateInstance_CardSetting_DefaultCardSettingId]
GO
ALTER TABLE [dbo].[User_TemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_TemplateInstance_TemplateInstance_TemplateInstanceId] FOREIGN KEY([TemplateInstanceId])
REFERENCES [dbo].[TemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[User_TemplateInstance] CHECK CONSTRAINT [FK_User_TemplateInstance_TemplateInstance_TemplateInstanceId]
GO
ALTER TABLE [dbo].[User_TemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_TemplateInstance_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[User_TemplateInstance] CHECK CONSTRAINT [FK_User_TemplateInstance_User_UserId]
GO
ALTER TABLE [dbo].[Vote_CommentCard]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentCard_CommentCard_CommentCardId] FOREIGN KEY([CommentCardId])
REFERENCES [dbo].[CommentCard] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentCard] CHECK CONSTRAINT [FK_Vote_CommentCard_CommentCard_CommentCardId]
GO
ALTER TABLE [dbo].[Vote_CommentCard]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentCard_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentCard] CHECK CONSTRAINT [FK_Vote_CommentCard_User_UserId]
GO
ALTER TABLE [dbo].[Vote_CommentTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentTemplate_CommentTemplate_CommentTemplateId] FOREIGN KEY([CommentTemplateId])
REFERENCES [dbo].[CommentTemplate] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentTemplate] CHECK CONSTRAINT [FK_Vote_CommentTemplate_CommentTemplate_CommentTemplateId]
GO
ALTER TABLE [dbo].[Vote_CommentTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentTemplate_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentTemplate] CHECK CONSTRAINT [FK_Vote_CommentTemplate_User_UserId]
GO
ALTER TABLE [dbo].[Vote_Feedback]  WITH CHECK ADD  CONSTRAINT [FK_Vote_Feedback_Feedback_FeedbackId] FOREIGN KEY([FeedbackId])
REFERENCES [dbo].[Feedback] ([Id])
GO
ALTER TABLE [dbo].[Vote_Feedback] CHECK CONSTRAINT [FK_Vote_Feedback_Feedback_FeedbackId]
GO
ALTER TABLE [dbo].[Vote_Feedback]  WITH CHECK ADD  CONSTRAINT [FK_Vote_Feedback_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_Feedback] CHECK CONSTRAINT [FK_Vote_Feedback_User_UserId]
GO
/****** Object:  Trigger [dbo].[TriggerCardInstanceUserUpdate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Alex
-- Create date: 11:40 10/8/2019
-- Description:	Updates the Users count of Card and CardInstance
-- =============================================
CREATE TRIGGER [dbo].[TriggerCardInstanceUserUpdate] 
	ON  [dbo].[AcquiredCard]
	AFTER INSERT, DELETE, UPDATE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF EXISTS (SELECT 1 FROM inserted)
	BEGIN
		UPDATE	dbo.CardInstance
		SET		Users = (SELECT Count(*) FROM dbo.AcquiredCard WHERE CardInstanceId = i.CardInstanceId)
		FROM	inserted i
		WHERE	dbo.CardInstance.Id = i.CardInstanceId
	END
	IF EXISTS (SELECT 1 FROM deleted)
	BEGIN
		UPDATE	dbo.CardInstance
		SET		Users = (SELECT Count(*) FROM dbo.AcquiredCard WHERE CardInstanceId = d.CardInstanceId)
		FROM	deleted d
		WHERE	dbo.CardInstance.Id = d.CardInstanceId
	END

	IF EXISTS (SELECT 1 FROM inserted)
	BEGIN
		UPDATE	c
		SET		Users =
				   (SELECT	SUM(ci2.Users)
					FROM	dbo.CardInstance ci2
					WHERE	ci2.CardId = c.Id)
		FROM	dbo.Card c
		INNER JOIN dbo.CardInstance ci
			ON ci.CardId = c.Id
		INNER JOIN inserted i
			ON ci.Id = i.CardInstanceId
	END
	IF EXISTS (SELECT 1 FROM deleted)
	BEGIN
		UPDATE	c
		SET		Users =
				   (SELECT	SUM(ci2.Users)
					FROM	dbo.CardInstance ci2
					WHERE	ci2.CardId = c.Id)
		FROM	dbo.Card c
		INNER JOIN dbo.CardInstance ci
			ON ci.CardId = c.Id
		INNER JOIN deleted d
			ON ci.Id = d.CardInstanceId
	END
END
GO
ALTER TABLE [dbo].[AcquiredCard] ENABLE TRIGGER [TriggerCardInstanceUserUpdate]
GO
USE [master]
GO
ALTER DATABASE [CardOverflow] SET  READ_WRITE 
GO

-- lowTODO: make a trigger to ensure that [dbo].[Relationship_AcquiredCard]'s AcquiredCard's UserIds are the same. Do *not* use a CHECK CONSTRAINT; those are unreliable
