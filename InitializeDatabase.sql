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
ALTER DATABASE [CardOverflow] SET TRUSTWORTHY OFF 
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
/****** Object:  Table [dbo].[AcquiredCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AcquiredCard](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MemorizationState] [tinyint] NOT NULL,
	[CardState] [tinyint] NOT NULL,
	[LapseCount] [tinyint] NOT NULL,
	[EaseFactorInPermille] [smallint] NOT NULL,
	[IntervalNegativeIsMinutesPositiveIsDays] [smallint] NOT NULL,
	[StepsIndex] [tinyint] NULL,
	[Due] [smalldatetime] NOT NULL,
	[CardOptionId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[ConceptInstanceId] [int] NOT NULL,
	[TemplateIndex] [int] NOT NULL,
 CONSTRAINT [PK_AcquiredCard] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CardOption] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CardOption](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[IsDefault] [bit] NOT NULL,
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
 CONSTRAINT [PK_CardOption] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Concept] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Concept](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaintainerId] [int] NOT NULL,
	[IsPublic] [bit] NOT NULL,
 CONSTRAINT [PK_Concept] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConceptComment] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConceptComment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ConceptId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_ConceptComment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConceptInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConceptInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Fields] [nvarchar](max) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Modified] [smalldatetime] NULL,
	[ConceptTemplateInstanceId] [int] NOT NULL,
	[ConceptId] [int] NOT NULL,
 CONSTRAINT [PK_ConceptInstance] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConceptTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConceptTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaintainerId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_ConceptTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConceptTemplate_ConceptTemplateDefault_User] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConceptTemplate_ConceptTemplateDefault_User](
	[ConceptTemplateId] [int] NOT NULL,
	[ConceptTemplateDefaultId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_ConceptTemplate_ConceptTemplateDefault_User] PRIMARY KEY CLUSTERED 
(
	[ConceptTemplateId] ASC,
	[ConceptTemplateDefaultId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConceptTemplateComment] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConceptTemplateComment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ConceptTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_ConceptTemplateComment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConceptTemplateDefault] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConceptTemplateDefault](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DefaultCardOptionId] [int] NOT NULL,
	[DefaultPrivateTags] [varchar](150) NOT NULL,
	[DefaultPublicTags] [varchar](150) NOT NULL,
 CONSTRAINT [PK_ConceptTemplateDefault] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConceptTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConceptTemplateInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ConceptTemplateId] [int] NOT NULL,
	[Css] [varchar](1000) NOT NULL,
	[Fields] [nvarchar](300) NOT NULL,
	[CardTemplates] [nvarchar](1000) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Modified] [smalldatetime] NULL,
	[IsCloze] [bit] NOT NULL,
	[LatexPre] [nvarchar](500) NOT NULL,
	[LatexPost] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_ConceptTemplateInstance] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Deck] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Deck](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[UserId] [int] NOT NULL,
	[Query] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Deck] PRIMARY KEY CLUSTERED 
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
	[FileName] [nvarchar](100) NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[Sha256] [binary](32) NOT NULL,
 CONSTRAINT [PK_File] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[File_ConceptInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[File_ConceptInstance](
	[ConceptInstanceId] [int] NOT NULL,
	[FileId] [int] NOT NULL,
 CONSTRAINT [PK_File_Concept] PRIMARY KEY CLUSTERED 
(
	[ConceptInstanceId] ASC,
	[FileId] ASC
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
	[MemorizationState] [tinyint] NOT NULL,
	[Timestamp] [smalldatetime] NOT NULL,
	[IntervalNegativeIsMinutesPositiveIsDays] [smallint] NOT NULL,
	[EaseFactorInPermille] [smallint] NOT NULL,
	[TimeFromSeeingQuestionToScoreInSecondsMinus32768] [smallint] NOT NULL,
 CONSTRAINT [PK_History] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PrivateTag] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PrivateTag](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_PrivateTag] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PrivateTag_AcquiredCard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PrivateTag_AcquiredCard](
	[PrivateTagId] [int] NOT NULL,
	[AcquiredCardId] [int] NOT NULL,
 CONSTRAINT [PK_PrivateTag_AcquiredCard] PRIMARY KEY CLUSTERED 
(
	[PrivateTagId] ASC,
	[AcquiredCardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PublicTag] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PublicTag](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_PublicTag] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PublicTag_Concept] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PublicTag_Concept](
	[ConceptId] [int] NOT NULL,
	[TemplateIndex] [tinyint] NOT NULL,
	[PublicTagId] [int] NOT NULL,
 CONSTRAINT [PK_PublicTag_Concept] PRIMARY KEY CLUSTERED 
(
	[ConceptId] ASC,
	[TemplateIndex] ASC,
	[PublicTagId] ASC
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
	[DisplayName] [nvarchar](32) NOT NULL,
	[Email] [nvarchar](254) NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_Concept] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_Concept](
	[ConceptId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_Concept] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[ConceptId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_ConceptComment] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_ConceptComment](
	[ConceptCommentId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_ConceptComment] PRIMARY KEY CLUSTERED 
(
	[ConceptCommentId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_ConceptTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_ConceptTemplate](
	[ConceptTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_ConceptTemplate] PRIMARY KEY CLUSTERED 
(
	[ConceptTemplateId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_ConceptTemplateComment] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_ConceptTemplateComment](
	[ConceptTemplateCommentId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_ConceptTemplateComment] PRIMARY KEY CLUSTERED 
(
	[ConceptTemplateCommentId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[CardOption] ON 

INSERT [dbo].[CardOption] ([Id], [UserId], [IsDefault], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (1, 1, 1, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
INSERT [dbo].[CardOption] ([Id], [UserId], [IsDefault], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (2, 2, 1, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
INSERT [dbo].[CardOption] ([Id], [UserId], [IsDefault], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (3, 3, 1, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
SET IDENTITY_INSERT [dbo].[CardOption] OFF
SET IDENTITY_INSERT [dbo].[ConceptTemplate] ON 

INSERT [dbo].[ConceptTemplate] ([Id], [MaintainerId], [Name]) VALUES (1, 2, N'Basic')
INSERT [dbo].[ConceptTemplate] ([Id], [MaintainerId], [Name]) VALUES (2, 2, N'Basic with reversed card')
INSERT [dbo].[ConceptTemplate] ([Id], [MaintainerId], [Name]) VALUES (3, 2, N'Basic with optional reversed card')
INSERT [dbo].[ConceptTemplate] ([Id], [MaintainerId], [Name]) VALUES (4, 2, N'Basic type in the answer')
INSERT [dbo].[ConceptTemplate] ([Id], [MaintainerId], [Name]) VALUES (5, 2, N'Basic Cloze')
SET IDENTITY_INSERT [dbo].[ConceptTemplate] OFF
INSERT [dbo].[ConceptTemplate_ConceptTemplateDefault_User] ([ConceptTemplateId], [ConceptTemplateDefaultId], [UserId]) VALUES (1, 1, 2)
INSERT [dbo].[ConceptTemplate_ConceptTemplateDefault_User] ([ConceptTemplateId], [ConceptTemplateDefaultId], [UserId]) VALUES (2, 2, 2)
INSERT [dbo].[ConceptTemplate_ConceptTemplateDefault_User] ([ConceptTemplateId], [ConceptTemplateDefaultId], [UserId]) VALUES (3, 3, 2)
INSERT [dbo].[ConceptTemplate_ConceptTemplateDefault_User] ([ConceptTemplateId], [ConceptTemplateDefaultId], [UserId]) VALUES (4, 4, 2)
INSERT [dbo].[ConceptTemplate_ConceptTemplateDefault_User] ([ConceptTemplateId], [ConceptTemplateDefaultId], [UserId]) VALUES (5, 5, 2)
SET IDENTITY_INSERT [dbo].[ConceptTemplateDefault] ON 

INSERT [dbo].[ConceptTemplateDefault] ([Id], [DefaultCardOptionId], [DefaultPrivateTags], [DefaultPublicTags]) VALUES (1, 2, N'', N'')
INSERT [dbo].[ConceptTemplateDefault] ([Id], [DefaultCardOptionId], [DefaultPrivateTags], [DefaultPublicTags]) VALUES (2, 2, N'', N'')
INSERT [dbo].[ConceptTemplateDefault] ([Id], [DefaultCardOptionId], [DefaultPrivateTags], [DefaultPublicTags]) VALUES (3, 2, N'', N'')
INSERT [dbo].[ConceptTemplateDefault] ([Id], [DefaultCardOptionId], [DefaultPrivateTags], [DefaultPublicTags]) VALUES (4, 2, N'', N'')
INSERT [dbo].[ConceptTemplateDefault] ([Id], [DefaultCardOptionId], [DefaultPrivateTags], [DefaultPublicTags]) VALUES (5, 2, N'', N'')
SET IDENTITY_INSERT [dbo].[ConceptTemplateDefault] OFF
SET IDENTITY_INSERT [dbo].[User] ON 

INSERT [dbo].[User] ([Id], [DisplayName], [Email]) VALUES (1, N'Admin', N'admin@cardoverflow.io')
INSERT [dbo].[User] ([Id], [DisplayName], [Email]) VALUES (2, N'The Collective', N'theCollective@cardoverflow.io')
INSERT [dbo].[User] ([Id], [DisplayName], [Email]) VALUES (3, N'RoboTurtle', N'roboturtle@cardoverflow.io')
SET IDENTITY_INSERT [dbo].[User] OFF
/****** Object:  Index [IX_AcquiredCard_CardOptionId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_CardOptionId] ON [dbo].[AcquiredCard]
(
	[CardOptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AcquiredCard_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_UserId] ON [dbo].[AcquiredCard]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [UQ_CardOption__UserId_IsDefault] ******/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_CardOption__UserId_IsDefault] ON [dbo].[CardOption]
(
	[UserId] ASC
)
WHERE ([IsDefault]=(1))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Concept_MaintainerId] ******/
CREATE NONCLUSTERED INDEX [IX_Concept_MaintainerId] ON [dbo].[Concept]
(
	[MaintainerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_ConceptTemplate_MaintainerId] ******/
CREATE NONCLUSTERED INDEX [IX_ConceptTemplate_MaintainerId] ON [dbo].[ConceptTemplate]
(
	[MaintainerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplateDefaultId] ******/
CREATE NONCLUSTERED INDEX [IX_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplateDefaultId] ON [dbo].[ConceptTemplate_ConceptTemplateDefault_User]
(
	[ConceptTemplateDefaultId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_ConceptTemplate_ConceptTemplateDefault_User_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_ConceptTemplate_ConceptTemplateDefault_User_UserId] ON [dbo].[ConceptTemplate_ConceptTemplateDefault_User]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_ConceptTemplateDefault_DefaultCardOptionId] ******/
CREATE NONCLUSTERED INDEX [IX_ConceptTemplateDefault_DefaultCardOptionId] ON [dbo].[ConceptTemplateDefault]
(
	[DefaultCardOptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deck_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Deck_UserId] ON [dbo].[Deck]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_File_Sha256] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_File_Sha256] ON [dbo].[File]
(
	[Sha256] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_File_Concept_FileId] ******/
CREATE NONCLUSTERED INDEX [IX_File_Concept_FileId] ON [dbo].[File_ConceptInstance]
(
	[FileId] ASC
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
/****** Object:  Index [AK_PrivateTag__UserId_Name] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_PrivateTag__UserId_Name] ON [dbo].[PrivateTag]
(
	[UserId] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [AK_PrivateTag_AcquiredCard] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_PrivateTag_AcquiredCard] ON [dbo].[PrivateTag_AcquiredCard]
(
	[AcquiredCardId] ASC,
	[PrivateTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_PublicTag__Name] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_PublicTag__Name] ON [dbo].[PublicTag]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_User__DisplayName] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_User__DisplayName] ON [dbo].[User]
(
	[DisplayName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_User__Email] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_User__Email] ON [dbo].[User]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_CardOption] FOREIGN KEY([CardOptionId])
REFERENCES [dbo].[CardOption] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_CardOption]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_ConceptInstance] FOREIGN KEY([ConceptInstanceId])
REFERENCES [dbo].[ConceptInstance] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_ConceptInstance]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_User]
GO
ALTER TABLE [dbo].[CardOption]  WITH CHECK ADD  CONSTRAINT [FK_CardOption_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CardOption] CHECK CONSTRAINT [FK_CardOption_User]
GO
ALTER TABLE [dbo].[Concept]  WITH CHECK ADD  CONSTRAINT [FK_Concept_User] FOREIGN KEY([MaintainerId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Concept] CHECK CONSTRAINT [FK_Concept_User]
GO
ALTER TABLE [dbo].[ConceptComment]  WITH CHECK ADD  CONSTRAINT [FK_ConceptComment_Concept] FOREIGN KEY([ConceptId])
REFERENCES [dbo].[Concept] ([Id])
GO
ALTER TABLE [dbo].[ConceptComment] CHECK CONSTRAINT [FK_ConceptComment_Concept]
GO
ALTER TABLE [dbo].[ConceptComment]  WITH CHECK ADD  CONSTRAINT [FK_ConceptComment_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[ConceptComment] CHECK CONSTRAINT [FK_ConceptComment_User]
GO
ALTER TABLE [dbo].[ConceptInstance]  WITH CHECK ADD  CONSTRAINT [FK_ConceptInstance_Concept] FOREIGN KEY([ConceptId])
REFERENCES [dbo].[Concept] ([Id])
GO
ALTER TABLE [dbo].[ConceptInstance] CHECK CONSTRAINT [FK_ConceptInstance_Concept]
GO
ALTER TABLE [dbo].[ConceptInstance]  WITH CHECK ADD  CONSTRAINT [FK_ConceptInstance_ConceptTemplateInstance] FOREIGN KEY([ConceptTemplateInstanceId])
REFERENCES [dbo].[ConceptTemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[ConceptInstance] CHECK CONSTRAINT [FK_ConceptInstance_ConceptTemplateInstance]
GO
ALTER TABLE [dbo].[ConceptTemplate]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplate_Maintainer] FOREIGN KEY([MaintainerId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplate] CHECK CONSTRAINT [FK_ConceptTemplate_Maintainer]
GO
ALTER TABLE [dbo].[ConceptTemplate_ConceptTemplateDefault_User]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplate] FOREIGN KEY([ConceptTemplateId])
REFERENCES [dbo].[ConceptTemplate] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplate_ConceptTemplateDefault_User] CHECK CONSTRAINT [FK_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplate]
GO
ALTER TABLE [dbo].[ConceptTemplate_ConceptTemplateDefault_User]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplateDefault] FOREIGN KEY([ConceptTemplateDefaultId])
REFERENCES [dbo].[ConceptTemplateDefault] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplate_ConceptTemplateDefault_User] CHECK CONSTRAINT [FK_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplateDefault]
GO
ALTER TABLE [dbo].[ConceptTemplate_ConceptTemplateDefault_User]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplate_ConceptTemplateDefault_User_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplate_ConceptTemplateDefault_User] CHECK CONSTRAINT [FK_ConceptTemplate_ConceptTemplateDefault_User_User]
GO
ALTER TABLE [dbo].[ConceptTemplateComment]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplateComment_ConceptTemplate] FOREIGN KEY([ConceptTemplateId])
REFERENCES [dbo].[ConceptTemplate] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplateComment] CHECK CONSTRAINT [FK_ConceptTemplateComment_ConceptTemplate]
GO
ALTER TABLE [dbo].[ConceptTemplateComment]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplateComment_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplateComment] CHECK CONSTRAINT [FK_ConceptTemplateComment_User]
GO
ALTER TABLE [dbo].[ConceptTemplateDefault]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplateDefault_CardOption] FOREIGN KEY([DefaultCardOptionId])
REFERENCES [dbo].[CardOption] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplateDefault] CHECK CONSTRAINT [FK_ConceptTemplateDefault_CardOption]
GO
ALTER TABLE [dbo].[ConceptTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_ConceptTemplateInstance_ConceptTemplate] FOREIGN KEY([ConceptTemplateId])
REFERENCES [dbo].[ConceptTemplate] ([Id])
GO
ALTER TABLE [dbo].[ConceptTemplateInstance] CHECK CONSTRAINT [FK_ConceptTemplateInstance_ConceptTemplate]
GO
ALTER TABLE [dbo].[Deck]  WITH CHECK ADD  CONSTRAINT [FK_Deck_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Deck] CHECK CONSTRAINT [FK_Deck_User]
GO
ALTER TABLE [dbo].[File_ConceptInstance]  WITH CHECK ADD  CONSTRAINT [FK_File_ConceptInstance_ConceptInstance] FOREIGN KEY([ConceptInstanceId])
REFERENCES [dbo].[ConceptInstance] ([Id])
GO
ALTER TABLE [dbo].[File_ConceptInstance] CHECK CONSTRAINT [FK_File_ConceptInstance_ConceptInstance]
GO
ALTER TABLE [dbo].[File_ConceptInstance]  WITH CHECK ADD  CONSTRAINT [FK_File_ConceptInstance_File] FOREIGN KEY([FileId])
REFERENCES [dbo].[File] ([Id])
GO
ALTER TABLE [dbo].[File_ConceptInstance] CHECK CONSTRAINT [FK_File_ConceptInstance_File]
GO
ALTER TABLE [dbo].[History]  WITH CHECK ADD  CONSTRAINT [FK_History_AcquiredCard] FOREIGN KEY([AcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
GO
ALTER TABLE [dbo].[History] CHECK CONSTRAINT [FK_History_AcquiredCard]
GO
ALTER TABLE [dbo].[PrivateTag]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[PrivateTag] CHECK CONSTRAINT [FK_PrivateTag_User]
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_AcquiredCard_AcquiredCard] FOREIGN KEY([AcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard] CHECK CONSTRAINT [FK_PrivateTag_AcquiredCard_AcquiredCard]
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_AcquiredCard_PrivateTag] FOREIGN KEY([PrivateTagId])
REFERENCES [dbo].[PrivateTag] ([Id])
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard] CHECK CONSTRAINT [FK_PrivateTag_AcquiredCard_PrivateTag]
GO
ALTER TABLE [dbo].[PublicTag_Concept]  WITH CHECK ADD  CONSTRAINT [FK_PublicTag_Concept_Concept] FOREIGN KEY([ConceptId])
REFERENCES [dbo].[Concept] ([Id])
GO
ALTER TABLE [dbo].[PublicTag_Concept] CHECK CONSTRAINT [FK_PublicTag_Concept_Concept]
GO
ALTER TABLE [dbo].[PublicTag_Concept]  WITH CHECK ADD  CONSTRAINT [FK_PublicTag_Concept_PublicTag] FOREIGN KEY([PublicTagId])
REFERENCES [dbo].[PublicTag] ([Id])
GO
ALTER TABLE [dbo].[PublicTag_Concept] CHECK CONSTRAINT [FK_PublicTag_Concept_PublicTag]
GO
ALTER TABLE [dbo].[Vote_Concept]  WITH CHECK ADD  CONSTRAINT [FK_Vote_Concept_Concept] FOREIGN KEY([ConceptId])
REFERENCES [dbo].[Concept] ([Id])
GO
ALTER TABLE [dbo].[Vote_Concept] CHECK CONSTRAINT [FK_Vote_Concept_Concept]
GO
ALTER TABLE [dbo].[Vote_Concept]  WITH CHECK ADD  CONSTRAINT [FK_Vote_Concept_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_Concept] CHECK CONSTRAINT [FK_Vote_Concept_User]
GO
ALTER TABLE [dbo].[Vote_ConceptComment]  WITH CHECK ADD  CONSTRAINT [FK_Vote_ConceptComment_ConceptComment] FOREIGN KEY([ConceptCommentId])
REFERENCES [dbo].[ConceptComment] ([Id])
GO
ALTER TABLE [dbo].[Vote_ConceptComment] CHECK CONSTRAINT [FK_Vote_ConceptComment_ConceptComment]
GO
ALTER TABLE [dbo].[Vote_ConceptComment]  WITH CHECK ADD  CONSTRAINT [FK_Vote_ConceptComment_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_ConceptComment] CHECK CONSTRAINT [FK_Vote_ConceptComment_User]
GO
ALTER TABLE [dbo].[Vote_ConceptTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_ConceptTemplate_ConceptTemplate] FOREIGN KEY([ConceptTemplateId])
REFERENCES [dbo].[ConceptTemplate] ([Id])
GO
ALTER TABLE [dbo].[Vote_ConceptTemplate] CHECK CONSTRAINT [FK_Vote_ConceptTemplate_ConceptTemplate]
GO
ALTER TABLE [dbo].[Vote_ConceptTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_ConceptTemplate_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_ConceptTemplate] CHECK CONSTRAINT [FK_Vote_ConceptTemplate_User]
GO
ALTER TABLE [dbo].[Vote_ConceptTemplateComment]  WITH CHECK ADD  CONSTRAINT [FK_Vote_ConceptTemplateComment_ConceptTemplateComment] FOREIGN KEY([ConceptTemplateCommentId])
REFERENCES [dbo].[ConceptTemplateComment] ([Id])
GO
ALTER TABLE [dbo].[Vote_ConceptTemplateComment] CHECK CONSTRAINT [FK_Vote_ConceptTemplateComment_ConceptTemplateComment]
GO
ALTER TABLE [dbo].[Vote_ConceptTemplateComment]  WITH CHECK ADD  CONSTRAINT [FK_Vote_ConceptTemplateComment_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_ConceptTemplateComment] CHECK CONSTRAINT [FK_Vote_ConceptTemplateComment_User]
GO
USE [master]
GO
ALTER DATABASE [CardOverflow] SET  READ_WRITE 
GO

