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
	[UserId] [int] NOT NULL,
	[CardInstanceId] [int] NOT NULL,
	[CardState] [tinyint] NOT NULL,
	[EaseFactorInPermille] [smallint] NOT NULL,
	[IntervalOrStepsIndex] [smallint] NOT NULL,
	[Due] [smalldatetime] NOT NULL,
	[CardOptionId] [int] NOT NULL,
	[IsLapsed] [bit] NOT NULL,
 CONSTRAINT [PK_AcquiredCard] PRIMARY KEY CLUSTERED 
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
/****** Object:  Table [dbo].[Card] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Card](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AuthorId] [int] NOT NULL,
	[Description] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Card] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CardInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CardInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Modified] [smalldatetime] NULL,
	[CardId] [int] NOT NULL,
	[AcquireHash] [binary](32) NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_CardInstance] PRIMARY KEY CLUSTERED 
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
/****** Object:  Table [dbo].[CardTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CardTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AuthorId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_CardTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CardTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CardTemplateInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CardTemplateId] [int] NOT NULL,
	[Css] [varchar](4000) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Modified] [smalldatetime] NULL,
	[LatexPre] [nvarchar](500) NOT NULL,
	[LatexPost] [nvarchar](500) NOT NULL,
	[AcquireHash] [binary](32) NOT NULL,
	[IsDmca] [bit] NOT NULL,
	[QuestionTemplate] [nvarchar](4000) NOT NULL,
	[AnswerTemplate] [nvarchar](4000) NOT NULL,
	[ShortQuestionTemplate] [nvarchar](200) NOT NULL,
	[ShortAnswerTemplate] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_CardTemplateInstance] PRIMARY KEY CLUSTERED 
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
/****** Object:  Table [dbo].[CommentCardTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommentCardTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CardTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_CommentCardTemplate] PRIMARY KEY CLUSTERED 
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
/****** Object:  Table [dbo].[Field] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Field](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Font] [nvarchar](100) NOT NULL,
	[FontSize] [tinyint] NOT NULL,
	[IsRightToLeft] [bit] NOT NULL,
	[Ordinal] [tinyint] NOT NULL,
	[IsSticky] [bit] NOT NULL,
	[CardTemplateInstanceId] [int] NOT NULL,
 CONSTRAINT [PK_Field] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FieldValue] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FieldValue](
	[CardInstanceId] [int] NOT NULL,
	[FieldId] [int] NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_FieldValue] PRIMARY KEY CLUSTERED 
(
	[CardInstanceId] ASC,
	[FieldId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
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
/****** Object:  Table [dbo].[Relationship] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Relationship](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SourceId] [int] NOT NULL,
	[TargetId] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_Relationship] PRIMARY KEY CLUSTERED 
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
/****** Object:  Table [dbo].[Tag_User_CardTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tag_User_CardTemplateInstance](
	[UserId] [int] NOT NULL,
	[CardTemplateInstanceId] [int] NOT NULL,
	[DefaultTagId] [int] NOT NULL,
 CONSTRAINT [PK_Tag_User_CardTemplateInstance] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[CardTemplateInstanceId] ASC,
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
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User_CardTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User_CardTemplateInstance](
	[UserId] [int] NOT NULL,
	[CardTemplateInstanceId] [int] NOT NULL,
	[DefaultCardOptionId] [int] NOT NULL,
 CONSTRAINT [PK_User_CardTemplateInstance] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[CardTemplateInstanceId] ASC
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
/****** Object:  Table [dbo].[Vote_CommentCardTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_CommentCardTemplate](
	[CommentCardTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_CommentCardTemplate] PRIMARY KEY CLUSTERED 
(
	[CommentCardTemplateId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[CardOption] ON 

INSERT [dbo].[CardOption] ([Id], [UserId], [IsDefault], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (1, 1, 1, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
INSERT [dbo].[CardOption] ([Id], [UserId], [IsDefault], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (2, 2, 1, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
INSERT [dbo].[CardOption] ([Id], [UserId], [IsDefault], [Name], [NewCardsStepsInMinutes], [NewCardsMaxPerDay], [NewCardsGraduatingIntervalInDays], [NewCardsEasyIntervalInDays], [NewCardsStartingEaseFactorInPermille], [NewCardsBuryRelated], [MatureCardsMaxPerDay], [MatureCardsEaseFactorEasyBonusFactorInPermille], [MatureCardsIntervalFactorInPermille], [MatureCardsMaximumIntervalInDays], [MatureCardsHardIntervalFactorInPermille], [MatureCardsBuryRelated], [LapsedCardsStepsInMinutes], [LapsedCardsNewIntervalFactorInPermille], [LapsedCardsMinimumIntervalInDays], [LapsedCardsLeechThreshold], [ShowAnswerTimer], [AutomaticallyPlayAudio], [ReplayQuestionAudioOnAnswer]) VALUES (3, 3, 1, N'Default', N'1 10', 20, 1, 4, 2500, 1, 200, 1300, 1000, 32767, 1200, 1, N'10', 0, 1, 8, 0, 0, 0)
SET IDENTITY_INSERT [dbo].[CardOption] OFF
SET IDENTITY_INSERT [dbo].[CardTemplate] ON 

INSERT [dbo].[CardTemplate] ([Id], [AuthorId], [Name]) VALUES (1, 2, N'Basic')
INSERT [dbo].[CardTemplate] ([Id], [AuthorId], [Name]) VALUES (2, 2, N'Basic (and reversed card) - Card 2')
INSERT [dbo].[CardTemplate] ([Id], [AuthorId], [Name]) VALUES (3, 2, N'Basic (optional reversed card) - Card 1')
INSERT [dbo].[CardTemplate] ([Id], [AuthorId], [Name]) VALUES (4, 2, N'Basic (optional reversed card) - Card 2')
INSERT [dbo].[CardTemplate] ([Id], [AuthorId], [Name]) VALUES (5, 2, N'Basic (type in the answer)')
INSERT [dbo].[CardTemplate] ([Id], [AuthorId], [Name]) VALUES (6, 2, N'Cloze')
SET IDENTITY_INSERT [dbo].[CardTemplate] OFF
SET IDENTITY_INSERT [dbo].[CardTemplateInstance] ON 

INSERT [dbo].[CardTemplateInstance] ([Id], [CardTemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [AcquireHash], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate]) VALUES (1, 1, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:54:00' AS SmallDateTime), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0x2A5FC38AF1420FF14113FC331E3429EBBF1C261D211F20A0BAE621BDC9E348FA, 0, N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'')
INSERT [dbo].[CardTemplateInstance] ([Id], [CardTemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [AcquireHash], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate]) VALUES (2, 2, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:51:00' AS SmallDateTime), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0x5DC9059C3535A11E090B0C5DCD88C87487C96AFEE8AB667AB879655FF30A76C9, 0, N'{{Back}}', N'{{FrontSide}}

<hr id=answer>

{{Front}}', N'', N'')
INSERT [dbo].[CardTemplateInstance] ([Id], [CardTemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [AcquireHash], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate]) VALUES (3, 3, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:52:00' AS SmallDateTime), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0xA4257D78D1CD3D2C41A8B1FD966E3F3EB936C0A1DCC8AC1988C469117DE43CC0, 0, N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'')
INSERT [dbo].[CardTemplateInstance] ([Id], [CardTemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [AcquireHash], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate]) VALUES (4, 4, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:52:00' AS SmallDateTime), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0xDDF5BD49CD518965D29555E5CEB4186F3C7D79FE0BF85E8FDA361C9D4F834E6E, 0, N'{{#Add Reverse}}{{Back}}{{/Add Reverse}}', N'{{FrontSide}}

<hr id=answer>

{{Front}}', N'', N'')
INSERT [dbo].[CardTemplateInstance] ([Id], [CardTemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [AcquireHash], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate]) VALUES (5, 5, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:52:00' AS SmallDateTime), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0x9FF44DBCBAD87D71DAC8825C0912C5FC5CD79F83B9876A8A2CAA762F6BA1F012, 0, N'{{Front}}
{{type:Back}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'')
INSERT [dbo].[CardTemplateInstance] ([Id], [CardTemplateId], [Css], [Created], [Modified], [LatexPre], [LatexPost], [AcquireHash], [IsDmca], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate]) VALUES (6, 6, N'.card {
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
}', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:52:00' AS SmallDateTime), N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0xA169E80F786DD7EF046CC587C8CCD78693474E2FB77991F125D278F7C7ABE5DD, 0, N'{{cloze:Text}}', N'{{cloze:Text}}<br>
{{Extra}}', N'', N'')
SET IDENTITY_INSERT [dbo].[CardTemplateInstance] OFF
SET IDENTITY_INSERT [dbo].[Field] ON 

INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (1, N'Front', N'Arial', 20, 0, 0, 0, 1)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (2, N'Back', N'Arial', 20, 0, 1, 0, 1)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (3, N'Text', N'Arial', 20, 0, 0, 0, 6)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (4, N'Front', N'Arial', 20, 0, 0, 0, 2)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (5, N'Back', N'Arial', 20, 0, 1, 0, 2)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (6, N'Front', N'Arial', 20, 0, 0, 0, 3)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (7, N'Back', N'Arial', 20, 0, 1, 0, 3)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (8, N'Add Reverse', N'Arial', 20, 0, 2, 0, 3)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (9, N'Extra', N'Arial', 20, 0, 1, 0, 6)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (10, N'Front', N'Arial', 20, 0, 0, 0, 4)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (11, N'Back', N'Arial', 20, 0, 1, 0, 4)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (12, N'Add Reverse', N'Arial', 20, 0, 2, 0, 4)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (13, N'Back', N'Arial', 20, 0, 1, 0, 5)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [CardTemplateInstanceId]) VALUES (14, N'Front', N'Arial', 20, 0, 0, 0, 5)
SET IDENTITY_INSERT [dbo].[Field] OFF
SET IDENTITY_INSERT [dbo].[User] ON 

INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName]) VALUES (1, NULL, NULL, N'admin@cardoverflow.io', NULL, 0, NULL, NULL, N'4934a9df-035b-4216-a8d7-cf00510a16ff', NULL, 0, 0, NULL, 0, 0, N'Admin')
INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName]) VALUES (2, NULL, NULL, N'theCollective@cardoverflow.io', NULL, 0, NULL, NULL, N'7f15011b-1605-4b2c-ba98-af5659739d60', NULL, 0, 0, NULL, 0, 0, N'The Collective')
INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName]) VALUES (3, NULL, NULL, N'roboturtle@cardoverflow.io', NULL, 0, NULL, NULL, N'd622b1ce-0c3b-48a3-9851-506e17bd04ec', NULL, 0, 0, NULL, 0, 0, N'RoboTurtle')
SET IDENTITY_INSERT [dbo].[User] OFF
INSERT [dbo].[User_CardTemplateInstance] ([UserId], [CardTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 1, 2)
INSERT [dbo].[User_CardTemplateInstance] ([UserId], [CardTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 2, 2)
INSERT [dbo].[User_CardTemplateInstance] ([UserId], [CardTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 3, 2)
INSERT [dbo].[User_CardTemplateInstance] ([UserId], [CardTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 4, 2)
INSERT [dbo].[User_CardTemplateInstance] ([UserId], [CardTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 5, 2)
INSERT [dbo].[User_CardTemplateInstance] ([UserId], [CardTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 6, 2)
/****** Object:  Index [IX_AcquiredCard_CardInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_CardInstanceId] ON [dbo].[AcquiredCard]
(
	[CardInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
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
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_CardInstance_AcquireHash] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_CardInstance_AcquireHash] ON [dbo].[CardInstance]
(
	[AcquireHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CardInstance_CardId] ******/
CREATE NONCLUSTERED INDEX [IX_CardInstance_CardId] ON [dbo].[CardInstance]
(
	[CardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CardOption_UserId] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_CardOption_UserId] ON [dbo].[CardOption]
(
	[UserId] ASC
)
WHERE ([IsDefault]=(1))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CardTemplate_AuthorId] ******/
CREATE NONCLUSTERED INDEX [IX_CardTemplate_AuthorId] ON [dbo].[CardTemplate]
(
	[AuthorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_CardTemplateInstance_AcquireHash] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_CardTemplateInstance_AcquireHash] ON [dbo].[CardTemplateInstance]
(
	[AcquireHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CardTemplateInstance_CardTemplateId] ******/
CREATE NONCLUSTERED INDEX [IX_CardTemplateInstance_CardTemplateId] ON [dbo].[CardTemplateInstance]
(
	[CardTemplateId] ASC
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
/****** Object:  Index [IX_CommentCardTemplate_CardTemplateId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentCardTemplate_CardTemplateId] ON [dbo].[CommentCardTemplate]
(
	[CardTemplateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentCardTemplate_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentCardTemplate_UserId] ON [dbo].[CommentCardTemplate]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deck_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Deck_UserId] ON [dbo].[Deck]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Field_CardTemplateInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_Field_CardTemplateInstanceId] ON [dbo].[Field]
(
	[CardTemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FieldValue_FieldId] ******/
CREATE NONCLUSTERED INDEX [IX_FieldValue_FieldId] ON [dbo].[FieldValue]
(
	[FieldId] ASC
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
/****** Object:  Index [IX_History_AcquiredCardId] ******/
CREATE NONCLUSTERED INDEX [IX_History_AcquiredCardId] ON [dbo].[History]
(
	[AcquiredCardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Relationship_SourceId] ******/
CREATE NONCLUSTERED INDEX [IX_Relationship_SourceId] ON [dbo].[Relationship]
(
	[SourceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Relationship_TargetId] ******/
CREATE NONCLUSTERED INDEX [IX_Relationship_TargetId] ON [dbo].[Relationship]
(
	[TargetId] ASC
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
/****** Object:  Index [IX_Tag_User_CardTemplateInstance_DefaultTagId] ******/
CREATE NONCLUSTERED INDEX [IX_Tag_User_CardTemplateInstance_DefaultTagId] ON [dbo].[Tag_User_CardTemplateInstance]
(
	[DefaultTagId] ASC
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
/****** Object:  Index [IX_User_CardTemplateInstance_CardTemplateInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_User_CardTemplateInstance_CardTemplateInstanceId] ON [dbo].[User_CardTemplateInstance]
(
	[CardTemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_User_CardTemplateInstance_DefaultCardOptionId] ******/
CREATE NONCLUSTERED INDEX [IX_User_CardTemplateInstance_DefaultCardOptionId] ON [dbo].[User_CardTemplateInstance]
(
	[DefaultCardOptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_CommentCard_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_CommentCard_UserId] ON [dbo].[Vote_CommentCard]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_CommentCardTemplate_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_CommentCardTemplate_UserId] ON [dbo].[Vote_CommentCardTemplate]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_CardInstance_CardInstanceId] FOREIGN KEY([CardInstanceId])
REFERENCES [dbo].[CardInstance] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_CardInstance_CardInstanceId]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_CardOption_CardOptionId] FOREIGN KEY([CardOptionId])
REFERENCES [dbo].[CardOption] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_CardOption_CardOptionId]
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
ALTER TABLE [dbo].[CardOption]  WITH CHECK ADD  CONSTRAINT [FK_CardOption_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CardOption] CHECK CONSTRAINT [FK_CardOption_User_UserId]
GO
ALTER TABLE [dbo].[CardTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CardTemplate_User_AuthorId] FOREIGN KEY([AuthorId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CardTemplate] CHECK CONSTRAINT [FK_CardTemplate_User_AuthorId]
GO
ALTER TABLE [dbo].[CardTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_CardTemplateInstance_CardTemplate_CardTemplateId] FOREIGN KEY([CardTemplateId])
REFERENCES [dbo].[CardTemplate] ([Id])
GO
ALTER TABLE [dbo].[CardTemplateInstance] CHECK CONSTRAINT [FK_CardTemplateInstance_CardTemplate_CardTemplateId]
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
ALTER TABLE [dbo].[CommentCardTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CommentCardTemplate_CardTemplate_CardTemplateId] FOREIGN KEY([CardTemplateId])
REFERENCES [dbo].[CardTemplate] ([Id])
GO
ALTER TABLE [dbo].[CommentCardTemplate] CHECK CONSTRAINT [FK_CommentCardTemplate_CardTemplate_CardTemplateId]
GO
ALTER TABLE [dbo].[CommentCardTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CommentCardTemplate_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CommentCardTemplate] CHECK CONSTRAINT [FK_CommentCardTemplate_User_UserId]
GO
ALTER TABLE [dbo].[Deck]  WITH CHECK ADD  CONSTRAINT [FK_Deck_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Deck] CHECK CONSTRAINT [FK_Deck_User_UserId]
GO
ALTER TABLE [dbo].[Field]  WITH CHECK ADD  CONSTRAINT [FK_Field_CardTemplateInstance_CardTemplateInstanceId] FOREIGN KEY([CardTemplateInstanceId])
REFERENCES [dbo].[CardTemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[Field] CHECK CONSTRAINT [FK_Field_CardTemplateInstance_CardTemplateInstanceId]
GO
ALTER TABLE [dbo].[FieldValue]  WITH CHECK ADD  CONSTRAINT [FK_FieldValue_CardInstance_CardInstanceId] FOREIGN KEY([CardInstanceId])
REFERENCES [dbo].[CardInstance] ([Id])
GO
ALTER TABLE [dbo].[FieldValue] CHECK CONSTRAINT [FK_FieldValue_CardInstance_CardInstanceId]
GO
ALTER TABLE [dbo].[FieldValue]  WITH CHECK ADD  CONSTRAINT [FK_FieldValue_Field_FieldId] FOREIGN KEY([FieldId])
REFERENCES [dbo].[Field] ([Id])
GO
ALTER TABLE [dbo].[FieldValue] CHECK CONSTRAINT [FK_FieldValue_Field_FieldId]
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
ALTER TABLE [dbo].[History]  WITH CHECK ADD  CONSTRAINT [FK_History_AcquiredCard_AcquiredCardId] FOREIGN KEY([AcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
GO
ALTER TABLE [dbo].[History] CHECK CONSTRAINT [FK_History_AcquiredCard_AcquiredCardId]
GO
ALTER TABLE [dbo].[Relationship]  WITH CHECK ADD  CONSTRAINT [FK_Relationship_Card_SourceId] FOREIGN KEY([SourceId])
REFERENCES [dbo].[Card] ([Id])
GO
ALTER TABLE [dbo].[Relationship] CHECK CONSTRAINT [FK_Relationship_Card_SourceId]
GO
ALTER TABLE [dbo].[Relationship]  WITH CHECK ADD  CONSTRAINT [FK_Relationship_Card_TargetId] FOREIGN KEY([TargetId])
REFERENCES [dbo].[Card] ([Id])
GO
ALTER TABLE [dbo].[Relationship] CHECK CONSTRAINT [FK_Relationship_Card_TargetId]
GO
ALTER TABLE [dbo].[Tag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_Tag_AcquiredCard_AcquiredCard_AcquiredCardId] FOREIGN KEY([AcquiredCardId])
REFERENCES [dbo].[AcquiredCard] ([Id])
GO
ALTER TABLE [dbo].[Tag_AcquiredCard] CHECK CONSTRAINT [FK_Tag_AcquiredCard_AcquiredCard_AcquiredCardId]
GO
ALTER TABLE [dbo].[Tag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_Tag_AcquiredCard_Tag_TagId] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tag] ([Id])
GO
ALTER TABLE [dbo].[Tag_AcquiredCard] CHECK CONSTRAINT [FK_Tag_AcquiredCard_Tag_TagId]
GO
ALTER TABLE [dbo].[Tag_User_CardTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_Tag_User_CardTemplateInstance_Tag_DefaultTagId] FOREIGN KEY([DefaultTagId])
REFERENCES [dbo].[Tag] ([Id])
GO
ALTER TABLE [dbo].[Tag_User_CardTemplateInstance] CHECK CONSTRAINT [FK_Tag_User_CardTemplateInstance_Tag_DefaultTagId]
GO
ALTER TABLE [dbo].[Tag_User_CardTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_Tag_User_CardTemplateInstance_User_CardTemplateInstance_UserId_CardTemplateInstanceId] FOREIGN KEY([UserId], [CardTemplateInstanceId])
REFERENCES [dbo].[User_CardTemplateInstance] ([UserId], [CardTemplateInstanceId])
GO
ALTER TABLE [dbo].[Tag_User_CardTemplateInstance] CHECK CONSTRAINT [FK_Tag_User_CardTemplateInstance_User_CardTemplateInstance_UserId_CardTemplateInstanceId]
GO
ALTER TABLE [dbo].[User_CardTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_CardTemplateInstance_CardOption_DefaultCardOptionId] FOREIGN KEY([DefaultCardOptionId])
REFERENCES [dbo].[CardOption] ([Id])
GO
ALTER TABLE [dbo].[User_CardTemplateInstance] CHECK CONSTRAINT [FK_User_CardTemplateInstance_CardOption_DefaultCardOptionId]
GO
ALTER TABLE [dbo].[User_CardTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_CardTemplateInstance_CardTemplateInstance_CardTemplateInstanceId] FOREIGN KEY([CardTemplateInstanceId])
REFERENCES [dbo].[CardTemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[User_CardTemplateInstance] CHECK CONSTRAINT [FK_User_CardTemplateInstance_CardTemplateInstance_CardTemplateInstanceId]
GO
ALTER TABLE [dbo].[User_CardTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_CardTemplateInstance_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[User_CardTemplateInstance] CHECK CONSTRAINT [FK_User_CardTemplateInstance_User_UserId]
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
ALTER TABLE [dbo].[Vote_CommentCardTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentCardTemplate_CommentCardTemplate_CommentCardTemplateId] FOREIGN KEY([CommentCardTemplateId])
REFERENCES [dbo].[CommentCardTemplate] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentCardTemplate] CHECK CONSTRAINT [FK_Vote_CommentCardTemplate_CommentCardTemplate_CommentCardTemplateId]
GO
ALTER TABLE [dbo].[Vote_CommentCardTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentCardTemplate_User_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentCardTemplate] CHECK CONSTRAINT [FK_Vote_CommentCardTemplate_User_UserId]
GO
USE [master]
GO
ALTER DATABASE [CardOverflow] SET  READ_WRITE 
GO

