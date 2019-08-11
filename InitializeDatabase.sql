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
	[UserId] [int] NOT NULL,
	[CardId] [int] NOT NULL,
	[MemorizationState] [tinyint] NOT NULL,
	[CardState] [tinyint] NOT NULL,
	[LapseCount] [tinyint] NOT NULL,
	[EaseFactorInPermille] [smallint] NOT NULL,
	[IntervalNegativeIsMinutesPositiveIsDays] [smallint] NOT NULL,
	[StepsIndex] [tinyint] NULL,
	[Due] [smalldatetime] NOT NULL,
	[CardOptionId] [int] NOT NULL,
 CONSTRAINT [PK_AcquiredCard] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[CardId] ASC
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
	[FacetInstanceId] [int] NOT NULL,
	[CardTemplateId] [int] NOT NULL,
	[ClozeIndex] [tinyint] NULL,
 CONSTRAINT [PK_Card] PRIMARY KEY CLUSTERED 
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
	[Name] [nvarchar](100) NOT NULL,
	[QuestionTemplate] [nvarchar](4000) NOT NULL,
	[AnswerTemplate] [nvarchar](4000) NOT NULL,
	[ShortQuestionTemplate] [nvarchar](200) NOT NULL,
	[ShortAnswerTemplate] [nvarchar](200) NOT NULL,
	[FacetTemplateInstanceId] [int] NOT NULL,
	[Ordinal] [tinyint] NOT NULL,
 CONSTRAINT [PK_CardTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommentFacet] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommentFacet](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FacetId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_CommentFacet] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommentFacetTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommentFacetTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FacetTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_CommentFacetTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Facet] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Facet](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaintainerId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Facet] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FacetInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FacetInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Modified] [smalldatetime] NULL,
	[FacetId] [int] NOT NULL,
	[AcquireHash] [binary](32) NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_FacetInstance] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FacetTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FacetTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaintainerId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_FacetTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FacetTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FacetTemplateInstance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FacetTemplateId] [int] NOT NULL,
	[Css] [varchar](4000) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Modified] [smalldatetime] NULL,
	[IsCloze] [bit] NOT NULL,
	[LatexPre] [nvarchar](500) NOT NULL,
	[LatexPost] [nvarchar](500) NOT NULL,
	[AcquireHash] [binary](32) NOT NULL,
	[IsDmca] [bit] NOT NULL,
 CONSTRAINT [PK_FacetTemplateInstance] PRIMARY KEY CLUSTERED 
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
	[FacetTemplateInstanceId] [int] NOT NULL,
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
	[FacetInstanceId] [int] NOT NULL,
	[FieldId] [int] NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_FieldValue] PRIMARY KEY CLUSTERED 
(
	[FacetInstanceId] ASC,
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
/****** Object:  Table [dbo].[File_FacetInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[File_FacetInstance](
	[FacetInstanceId] [int] NOT NULL,
	[FileId] [int] NOT NULL,
 CONSTRAINT [PK_File_Facet] PRIMARY KEY CLUSTERED 
(
	[FacetInstanceId] ASC,
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
	[UserId] [int] NOT NULL,
	[CardId] [int] NOT NULL,
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
	[UserId] [int] NOT NULL,
	[CardId] [int] NOT NULL,
 CONSTRAINT [PK_PrivateTag_AcquiredCard] PRIMARY KEY CLUSTERED 
(
	[PrivateTagId] ASC,
	[UserId] ASC,
	[CardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PrivateTag_User_FacetTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PrivateTag_User_FacetTemplateInstance](
	[UserId] [int] NOT NULL,
	[FacetTemplateInstanceId] [int] NOT NULL,
	[DefaultPrivateTagId] [int] NOT NULL,
 CONSTRAINT [PK_PrivateTag_User_FacetTemplateInstance] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[FacetTemplateInstanceId] ASC,
	[DefaultPrivateTagId] ASC
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
/****** Object:  Table [dbo].[PublicTag_Facet] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PublicTag_Facet](
	[FacetId] [int] NOT NULL,
	[PublicTagId] [int] NOT NULL,
 CONSTRAINT [PK_PublicTag_Facet] PRIMARY KEY CLUSTERED 
(
	[FacetId] ASC,
	[PublicTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PublicTag_User_FacetTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PublicTag_User_FacetTemplateInstance](
	[UserId] [int] NOT NULL,
	[FacetTemplateInstanceId] [int] NOT NULL,
	[DefaultPublicTagId] [int] NOT NULL,
 CONSTRAINT [PK_PublicTag_User_FacetTemplateInstance] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[FacetTemplateInstanceId] ASC,
	[DefaultPublicTagId] ASC
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
/****** Object:  Table [dbo].[User_FacetTemplateInstance] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User_FacetTemplateInstance](
	[UserId] [int] NOT NULL,
	[FacetTemplateInstanceId] [int] NOT NULL,
	[DefaultCardOptionId] [int] NOT NULL,
 CONSTRAINT [PK_User_FacetTemplateInstance] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[FacetTemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_CommentFacet] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_CommentFacet](
	[CommentFacetId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_CommentFacet] PRIMARY KEY CLUSTERED 
(
	[CommentFacetId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_CommentFacetTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_CommentFacetTemplate](
	[CommentFacetTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_CommentFacetTemplate] PRIMARY KEY CLUSTERED 
(
	[CommentFacetTemplateId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_Facet] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_Facet](
	[FacetId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_Facet] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[FacetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vote_FacetTemplate] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vote_FacetTemplate](
	[FacetTemplateId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Vote_FacetTemplate] PRIMARY KEY CLUSTERED 
(
	[FacetTemplateId] ASC,
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

INSERT [dbo].[CardTemplate] ([Id], [Name], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [FacetTemplateInstanceId], [Ordinal]) VALUES (1, N'Card 1', N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', 1, 0)
INSERT [dbo].[CardTemplate] ([Id], [Name], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [FacetTemplateInstanceId], [Ordinal]) VALUES (2, N'Cloze', N'{{cloze:Text}}', N'{{cloze:Text}}<br>
{{Extra}}', N'', N'', 5, 0)
INSERT [dbo].[CardTemplate] ([Id], [Name], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [FacetTemplateInstanceId], [Ordinal]) VALUES (3, N'Card 1', N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', 2, 0)
INSERT [dbo].[CardTemplate] ([Id], [Name], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [FacetTemplateInstanceId], [Ordinal]) VALUES (4, N'Card 2', N'{{Back}}', N'{{FrontSide}}

<hr id=answer>

{{Front}}', N'', N'', 2, 1)
INSERT [dbo].[CardTemplate] ([Id], [Name], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [FacetTemplateInstanceId], [Ordinal]) VALUES (5, N'Card 1', N'{{Front}}
{{type:Back}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', 4, 0)
INSERT [dbo].[CardTemplate] ([Id], [Name], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [FacetTemplateInstanceId], [Ordinal]) VALUES (6, N'Card 1', N'{{Front}}', N'{{FrontSide}}

<hr id=answer>

{{Back}}', N'', N'', 3, 0)
INSERT [dbo].[CardTemplate] ([Id], [Name], [QuestionTemplate], [AnswerTemplate], [ShortQuestionTemplate], [ShortAnswerTemplate], [FacetTemplateInstanceId], [Ordinal]) VALUES (7, N'Card 2', N'{{#Add Reverse}}{{Back}}{{/Add Reverse}}', N'{{FrontSide}}

<hr id=answer>

{{Front}}', N'', N'', 3, 1)
SET IDENTITY_INSERT [dbo].[CardTemplate] OFF
SET IDENTITY_INSERT [dbo].[FacetTemplate] ON 

INSERT [dbo].[FacetTemplate] ([Id], [MaintainerId], [Name]) VALUES (1, 2, N'Basic')
INSERT [dbo].[FacetTemplate] ([Id], [MaintainerId], [Name]) VALUES (2, 2, N'Basic (and reversed card)')
INSERT [dbo].[FacetTemplate] ([Id], [MaintainerId], [Name]) VALUES (3, 2, N'Basic (optional reversed card)')
INSERT [dbo].[FacetTemplate] ([Id], [MaintainerId], [Name]) VALUES (4, 2, N'Basic (type in the answer)')
INSERT [dbo].[FacetTemplate] ([Id], [MaintainerId], [Name]) VALUES (5, 2, N'Cloze')
SET IDENTITY_INSERT [dbo].[FacetTemplate] OFF
SET IDENTITY_INSERT [dbo].[FacetTemplateInstance] ON 

INSERT [dbo].[FacetTemplateInstance] ([Id], [FacetTemplateId], [Css], [Created], [Modified], [IsCloze], [LatexPre], [LatexPost], [AcquireHash], [IsDmca]) VALUES (1, 1, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:54:00' AS SmallDateTime), 0, N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0x60493569DF91D284A8C43CF4FCA5DAC826BD7A98F778616298E2BB10ED95378A, 0)
INSERT [dbo].[FacetTemplateInstance] ([Id], [FacetTemplateId], [Css], [Created], [Modified], [IsCloze], [LatexPre], [LatexPost], [AcquireHash], [IsDmca]) VALUES (2, 2, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:51:00' AS SmallDateTime), 0, N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0x4643599FEF0CECFBADB3DA654DA2B0587B89DE0DCAFBE04537768AAB0ECA3316, 0)
INSERT [dbo].[FacetTemplateInstance] ([Id], [FacetTemplateId], [Css], [Created], [Modified], [IsCloze], [LatexPre], [LatexPost], [AcquireHash], [IsDmca]) VALUES (3, 3, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:52:00' AS SmallDateTime), 0, N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0x037F71E478A4286CA3CF5C50044B0CF5017BBF77C47CDA99F4FAB5873656B608, 0)
INSERT [dbo].[FacetTemplateInstance] ([Id], [FacetTemplateId], [Css], [Created], [Modified], [IsCloze], [LatexPre], [LatexPost], [AcquireHash], [IsDmca]) VALUES (4, 4, N'.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}
', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:52:00' AS SmallDateTime), 0, N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0xA18D04E6E681021987B7BB9B6F4D2247A8CF479B2CFBA276C9E3E49D8EE253E8, 0)
INSERT [dbo].[FacetTemplateInstance] ([Id], [FacetTemplateId], [Css], [Created], [Modified], [IsCloze], [LatexPre], [LatexPost], [AcquireHash], [IsDmca]) VALUES (5, 5, N'.card {
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
}', CAST(N'2019-04-08T02:14:00' AS SmallDateTime), CAST(N'2019-06-16T00:52:00' AS SmallDateTime), 1, N'\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
', N'\end{document}', 0x41F84217C80C0918F95729E8704B7C5288630B2217C6E1D7D4C7BB4933C8D5FC, 0)
SET IDENTITY_INSERT [dbo].[FacetTemplateInstance] OFF
SET IDENTITY_INSERT [dbo].[Field] ON 

INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (1, N'Text', N'Arial', 20, 0, 0, 0, 5)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (2, N'Back', N'Arial', 20, 0, 1, 0, 4)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (3, N'Front', N'Arial', 20, 0, 0, 0, 4)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (4, N'Add Reverse', N'Arial', 20, 0, 2, 0, 3)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (5, N'Back', N'Arial', 20, 0, 1, 0, 3)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (6, N'Front', N'Arial', 20, 0, 0, 0, 3)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (7, N'Back', N'Arial', 20, 0, 1, 0, 2)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (8, N'Front', N'Arial', 20, 0, 0, 0, 2)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (9, N'Back', N'Arial', 20, 0, 1, 0, 1)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (10, N'Front', N'Arial', 20, 0, 0, 0, 1)
INSERT [dbo].[Field] ([Id], [Name], [Font], [FontSize], [IsRightToLeft], [Ordinal], [IsSticky], [FacetTemplateInstanceId]) VALUES (11, N'Extra', N'Arial', 20, 0, 1, 0, 5)
SET IDENTITY_INSERT [dbo].[Field] OFF
SET IDENTITY_INSERT [dbo].[User] ON 

INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName]) VALUES (1, NULL, NULL, N'admin@cardoverflow.io', NULL, 0, NULL, NULL, N'3cb002ab-17d6-453f-860a-f86feb7e08d5', NULL, 0, 0, NULL, 0, 0, N'Admin')
INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName]) VALUES (2, NULL, NULL, N'theCollective@cardoverflow.io', NULL, 0, NULL, NULL, N'79d35774-8255-4459-a7b2-76c6d28682f6', NULL, 0, 0, NULL, 0, 0, N'The Collective')
INSERT [dbo].[User] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName]) VALUES (3, NULL, NULL, N'roboturtle@cardoverflow.io', NULL, 0, NULL, NULL, N'61eebfe3-e552-4f00-9e3f-877d8dade807', NULL, 0, 0, NULL, 0, 0, N'RoboTurtle')
SET IDENTITY_INSERT [dbo].[User] OFF
INSERT [dbo].[User_FacetTemplateInstance] ([UserId], [FacetTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 1, 2)
INSERT [dbo].[User_FacetTemplateInstance] ([UserId], [FacetTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 2, 2)
INSERT [dbo].[User_FacetTemplateInstance] ([UserId], [FacetTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 3, 2)
INSERT [dbo].[User_FacetTemplateInstance] ([UserId], [FacetTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 4, 2)
INSERT [dbo].[User_FacetTemplateInstance] ([UserId], [FacetTemplateInstanceId], [DefaultCardOptionId]) VALUES (2, 5, 2)
/****** Object:  Index [IX_AcquiredCard_CardId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_CardId] ON [dbo].[AcquiredCard]
(
	[CardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_AcquiredCard_CardOptionId] ******/
CREATE NONCLUSTERED INDEX [IX_AcquiredCard_CardOptionId] ON [dbo].[AcquiredCard]
(
	[CardOptionId] ASC
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
/****** Object:  Index [AK_Card] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_Card] ON [dbo].[Card]
(
	[FacetInstanceId] ASC,
	[CardTemplateId] ASC,
	[ClozeIndex] ASC
)

WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Card_CardTemplateId] ******/
CREATE NONCLUSTERED INDEX [IX_Card_CardTemplateId] ON [dbo].[Card]
(
	[CardTemplateId] ASC
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
/****** Object:  Index [IX_CardTemplate_FacetTemplateInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_CardTemplate_FacetTemplateInstanceId] ON [dbo].[CardTemplate]
(
	[FacetTemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentFacet_FacetId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentFacet_FacetId] ON [dbo].[CommentFacet]
(
	[FacetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentFacet_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentFacet_UserId] ON [dbo].[CommentFacet]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentFacetTemplate_FacetTemplateId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentFacetTemplate_FacetTemplateId] ON [dbo].[CommentFacetTemplate]
(
	[FacetTemplateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentFacetTemplate_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_CommentFacetTemplate_UserId] ON [dbo].[CommentFacetTemplate]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Facet_MaintainerId] ******/
CREATE NONCLUSTERED INDEX [IX_Facet_MaintainerId] ON [dbo].[Facet]
(
	[MaintainerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_FacetInstance_AcquireHash] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_FacetInstance_AcquireHash] ON [dbo].[FacetInstance]
(
	[AcquireHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FacetInstance_FacetId] ******/
CREATE NONCLUSTERED INDEX [IX_FacetInstance_FacetId] ON [dbo].[FacetInstance]
(
	[FacetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FacetTemplate_MaintainerId] ******/
CREATE NONCLUSTERED INDEX [IX_FacetTemplate_MaintainerId] ON [dbo].[FacetTemplate]
(
	[MaintainerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_FacetTemplateInstance_AcquireHash] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_FacetTemplateInstance_AcquireHash] ON [dbo].[FacetTemplateInstance]
(
	[AcquireHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FacetTemplateInstance_FacetTemplateId] ******/
CREATE NONCLUSTERED INDEX [IX_FacetTemplateInstance_FacetTemplateId] ON [dbo].[FacetTemplateInstance]
(
	[FacetTemplateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deck_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Deck_UserId] ON [dbo].[Deck]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Field_FacetTemplateInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_Field_FacetTemplateInstanceId] ON [dbo].[Field]
(
	[FacetTemplateInstanceId] ASC
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
/****** Object:  Index [AK_File_Sha256] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_File_Sha256] ON [dbo].[File]
(
	[Sha256] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_File_Facet_FileId] ******/
CREATE NONCLUSTERED INDEX [IX_File_Facet_FileId] ON [dbo].[File_FacetInstance]
(
	[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_History_UserId_CardId] ******/
CREATE NONCLUSTERED INDEX [IX_History_UserId_CardId] ON [dbo].[History]
(
	[UserId] ASC,
	[CardId] ASC
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
/****** Object:  Index [IX_PrivateTag_AcquiredCard_UserId_CardId] ******/
CREATE NONCLUSTERED INDEX [IX_PrivateTag_AcquiredCard_UserId_CardId] ON [dbo].[PrivateTag_AcquiredCard]
(
	[UserId] ASC,
	[CardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_PrivateTag_User_FacetTemplateInstance_DefaultPrivateTagId] ******/
CREATE NONCLUSTERED INDEX [IX_PrivateTag_User_FacetTemplateInstance_DefaultPrivateTagId] ON [dbo].[PrivateTag_User_FacetTemplateInstance]
(
	[DefaultPrivateTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_PublicTag__Name] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_PublicTag__Name] ON [dbo].[PublicTag]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_PublicTag_Facet_PublicTagId] ******/
CREATE NONCLUSTERED INDEX [IX_PublicTag_Facet_PublicTagId] ON [dbo].[PublicTag_Facet]
(
	[PublicTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_PublicTag_User_FacetTemplateInstance_DefaultPublicTagId] ******/
CREATE NONCLUSTERED INDEX [IX_PublicTag_User_FacetTemplateInstance_DefaultPublicTagId] ON [dbo].[PublicTag_User_FacetTemplateInstance]
(
	[DefaultPublicTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_User__DisplayName] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_User__DisplayName] ON [dbo].[User]
(
	[DisplayName] ASC
)
WHERE ([DisplayName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [AK_User__Email] ******/
CREATE UNIQUE NONCLUSTERED INDEX [AK_User__Email] ON [dbo].[User]
(
	[Email] ASC
)
WHERE ([Email] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
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
/****** Object:  Index [UserNameIndex] ******/
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[User]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FacetTemplateDefault_DefaultCardOptionId] ******/
CREATE NONCLUSTERED INDEX [IX_FacetTemplateDefault_DefaultCardOptionId] ON [dbo].[User_FacetTemplateInstance]
(
	[DefaultCardOptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_User_FacetTemplateInstance_FacetTemplateInstanceId] ******/
CREATE NONCLUSTERED INDEX [IX_User_FacetTemplateInstance_FacetTemplateInstanceId] ON [dbo].[User_FacetTemplateInstance]
(
	[FacetTemplateInstanceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_CommentFacet_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_CommentFacet_UserId] ON [dbo].[Vote_CommentFacet]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_CommentFacetTemplate_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_CommentFacetTemplate_UserId] ON [dbo].[Vote_CommentFacetTemplate]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_Facet_FacetId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_Facet_FacetId] ON [dbo].[Vote_Facet]
(
	[FacetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Vote_FacetTemplate_UserId] ******/
CREATE NONCLUSTERED INDEX [IX_Vote_FacetTemplate_UserId] ON [dbo].[Vote_FacetTemplate]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[CommentFacet] ADD  CONSTRAINT [DF_CommentFacet_IsDmca]  DEFAULT ((0)) FOR [IsDmca]
GO
ALTER TABLE [dbo].[CommentFacetTemplate] ADD  CONSTRAINT [DF_CommentFacetTemplate_IsDmca]  DEFAULT ((0)) FOR [IsDmca]
GO
ALTER TABLE [dbo].[FacetInstance] ADD  CONSTRAINT [DF_FacetInstance_IsDmca]  DEFAULT ((0)) FOR [IsDmca]
GO
ALTER TABLE [dbo].[FacetTemplateInstance] ADD  CONSTRAINT [DF_FacetTemplateInstance_IsDmca]  DEFAULT ((0)) FOR [IsDmca]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_Card] FOREIGN KEY([CardId])
REFERENCES [dbo].[Card] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_Card]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_CardOption] FOREIGN KEY([CardOptionId])
REFERENCES [dbo].[CardOption] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_CardOption]
GO
ALTER TABLE [dbo].[AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_AcquiredCard_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[AcquiredCard] CHECK CONSTRAINT [FK_AcquiredCard_User]
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
ALTER TABLE [dbo].[Card]  WITH CHECK ADD  CONSTRAINT [FK_Card_CardTemplate] FOREIGN KEY([CardTemplateId])
REFERENCES [dbo].[CardTemplate] ([Id])
GO
ALTER TABLE [dbo].[Card] CHECK CONSTRAINT [FK_Card_CardTemplate]
GO
ALTER TABLE [dbo].[Card]  WITH CHECK ADD  CONSTRAINT [FK_Card_FacetInstance] FOREIGN KEY([FacetInstanceId])
REFERENCES [dbo].[FacetInstance] ([Id])
GO
ALTER TABLE [dbo].[Card] CHECK CONSTRAINT [FK_Card_FacetInstance]
GO
ALTER TABLE [dbo].[CardOption]  WITH CHECK ADD  CONSTRAINT [FK_CardOption_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CardOption] CHECK CONSTRAINT [FK_CardOption_User]
GO
ALTER TABLE [dbo].[CardTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CardTemplate_FacetTemplateInstance] FOREIGN KEY([FacetTemplateInstanceId])
REFERENCES [dbo].[FacetTemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[CardTemplate] CHECK CONSTRAINT [FK_CardTemplate_FacetTemplateInstance]
GO
ALTER TABLE [dbo].[CommentFacet]  WITH CHECK ADD  CONSTRAINT [FK_CommentFacet_Facet] FOREIGN KEY([FacetId])
REFERENCES [dbo].[Facet] ([Id])
GO
ALTER TABLE [dbo].[CommentFacet] CHECK CONSTRAINT [FK_CommentFacet_Facet]
GO
ALTER TABLE [dbo].[CommentFacet]  WITH CHECK ADD  CONSTRAINT [FK_CommentFacet_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CommentFacet] CHECK CONSTRAINT [FK_CommentFacet_User]
GO
ALTER TABLE [dbo].[CommentFacetTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CommentFacetTemplate_FacetTemplate] FOREIGN KEY([FacetTemplateId])
REFERENCES [dbo].[FacetTemplate] ([Id])
GO
ALTER TABLE [dbo].[CommentFacetTemplate] CHECK CONSTRAINT [FK_CommentFacetTemplate_FacetTemplate]
GO
ALTER TABLE [dbo].[CommentFacetTemplate]  WITH CHECK ADD  CONSTRAINT [FK_CommentFacetTemplate_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[CommentFacetTemplate] CHECK CONSTRAINT [FK_CommentFacetTemplate_User]
GO
ALTER TABLE [dbo].[Facet]  WITH CHECK ADD  CONSTRAINT [FK_Facet_User] FOREIGN KEY([MaintainerId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Facet] CHECK CONSTRAINT [FK_Facet_User]
GO
ALTER TABLE [dbo].[FacetInstance]  WITH CHECK ADD  CONSTRAINT [FK_FacetInstance_Facet] FOREIGN KEY([FacetId])
REFERENCES [dbo].[Facet] ([Id])
GO
ALTER TABLE [dbo].[FacetInstance] CHECK CONSTRAINT [FK_FacetInstance_Facet]
GO
ALTER TABLE [dbo].[FacetTemplate]  WITH CHECK ADD  CONSTRAINT [FK_FacetTemplate_Maintainer] FOREIGN KEY([MaintainerId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[FacetTemplate] CHECK CONSTRAINT [FK_FacetTemplate_Maintainer]
GO
ALTER TABLE [dbo].[FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_FacetTemplateInstance_FacetTemplate] FOREIGN KEY([FacetTemplateId])
REFERENCES [dbo].[FacetTemplate] ([Id])
GO
ALTER TABLE [dbo].[FacetTemplateInstance] CHECK CONSTRAINT [FK_FacetTemplateInstance_FacetTemplate]
GO
ALTER TABLE [dbo].[Deck]  WITH CHECK ADD  CONSTRAINT [FK_Deck_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Deck] CHECK CONSTRAINT [FK_Deck_User]
GO
ALTER TABLE [dbo].[Field]  WITH CHECK ADD  CONSTRAINT [FK_Field_FacetTemplateInstance] FOREIGN KEY([FacetTemplateInstanceId])
REFERENCES [dbo].[FacetTemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[Field] CHECK CONSTRAINT [FK_Field_FacetTemplateInstance]
GO
ALTER TABLE [dbo].[FieldValue]  WITH CHECK ADD  CONSTRAINT [FK_FieldValue_FacetInstance] FOREIGN KEY([FacetInstanceId])
REFERENCES [dbo].[FacetInstance] ([Id])
GO
ALTER TABLE [dbo].[FieldValue] CHECK CONSTRAINT [FK_FieldValue_FacetInstance]
GO
ALTER TABLE [dbo].[FieldValue]  WITH CHECK ADD  CONSTRAINT [FK_FieldValue_Field] FOREIGN KEY([FieldId])
REFERENCES [dbo].[Field] ([Id])
GO
ALTER TABLE [dbo].[FieldValue] CHECK CONSTRAINT [FK_FieldValue_Field]
GO
ALTER TABLE [dbo].[File_FacetInstance]  WITH CHECK ADD  CONSTRAINT [FK_File_FacetInstance_FacetInstance] FOREIGN KEY([FacetInstanceId])
REFERENCES [dbo].[FacetInstance] ([Id])
GO
ALTER TABLE [dbo].[File_FacetInstance] CHECK CONSTRAINT [FK_File_FacetInstance_FacetInstance]
GO
ALTER TABLE [dbo].[File_FacetInstance]  WITH CHECK ADD  CONSTRAINT [FK_File_FacetInstance_File] FOREIGN KEY([FileId])
REFERENCES [dbo].[File] ([Id])
GO
ALTER TABLE [dbo].[File_FacetInstance] CHECK CONSTRAINT [FK_File_FacetInstance_File]
GO
ALTER TABLE [dbo].[History]  WITH CHECK ADD  CONSTRAINT [FK_History_AcquiredCard] FOREIGN KEY([UserId], [CardId])
REFERENCES [dbo].[AcquiredCard] ([UserId], [CardId])
GO
ALTER TABLE [dbo].[History] CHECK CONSTRAINT [FK_History_AcquiredCard]
GO
ALTER TABLE [dbo].[PrivateTag]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[PrivateTag] CHECK CONSTRAINT [FK_PrivateTag_User]
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_AcquiredCard_AcquiredCard] FOREIGN KEY([UserId], [CardId])
REFERENCES [dbo].[AcquiredCard] ([UserId], [CardId])
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard] CHECK CONSTRAINT [FK_PrivateTag_AcquiredCard_AcquiredCard]
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_AcquiredCard_PrivateTag] FOREIGN KEY([PrivateTagId])
REFERENCES [dbo].[PrivateTag] ([Id])
GO
ALTER TABLE [dbo].[PrivateTag_AcquiredCard] CHECK CONSTRAINT [FK_PrivateTag_AcquiredCard_PrivateTag]
GO
ALTER TABLE [dbo].[PrivateTag_User_FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_User_FacetTemplateInstance_PrivateTag] FOREIGN KEY([DefaultPrivateTagId])
REFERENCES [dbo].[PrivateTag] ([Id])
GO
ALTER TABLE [dbo].[PrivateTag_User_FacetTemplateInstance] CHECK CONSTRAINT [FK_PrivateTag_User_FacetTemplateInstance_PrivateTag]
GO
ALTER TABLE [dbo].[PrivateTag_User_FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_PrivateTag_User_FacetTemplateInstance_User_FacetTemplateInstance] FOREIGN KEY([UserId], [FacetTemplateInstanceId])
REFERENCES [dbo].[User_FacetTemplateInstance] ([UserId], [FacetTemplateInstanceId])
GO
ALTER TABLE [dbo].[PrivateTag_User_FacetTemplateInstance] CHECK CONSTRAINT [FK_PrivateTag_User_FacetTemplateInstance_User_FacetTemplateInstance]
GO
ALTER TABLE [dbo].[PublicTag_Facet]  WITH CHECK ADD  CONSTRAINT [FK_PublicTag_Facet_Facet] FOREIGN KEY([FacetId])
REFERENCES [dbo].[Facet] ([Id])
GO
ALTER TABLE [dbo].[PublicTag_Facet] CHECK CONSTRAINT [FK_PublicTag_Facet_Facet]
GO
ALTER TABLE [dbo].[PublicTag_Facet]  WITH CHECK ADD  CONSTRAINT [FK_PublicTag_Facet_PublicTag] FOREIGN KEY([PublicTagId])
REFERENCES [dbo].[PublicTag] ([Id])
GO
ALTER TABLE [dbo].[PublicTag_Facet] CHECK CONSTRAINT [FK_PublicTag_Facet_PublicTag]
GO
ALTER TABLE [dbo].[PublicTag_User_FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_PublicTag_User_FacetTemplateInstance_PublicTag] FOREIGN KEY([DefaultPublicTagId])
REFERENCES [dbo].[PublicTag] ([Id])
GO
ALTER TABLE [dbo].[PublicTag_User_FacetTemplateInstance] CHECK CONSTRAINT [FK_PublicTag_User_FacetTemplateInstance_PublicTag]
GO
ALTER TABLE [dbo].[PublicTag_User_FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_PublicTag_User_FacetTemplateInstance_User_FacetTemplateInstance] FOREIGN KEY([UserId], [FacetTemplateInstanceId])
REFERENCES [dbo].[User_FacetTemplateInstance] ([UserId], [FacetTemplateInstanceId])
GO
ALTER TABLE [dbo].[PublicTag_User_FacetTemplateInstance] CHECK CONSTRAINT [FK_PublicTag_User_FacetTemplateInstance_User_FacetTemplateInstance]
GO
ALTER TABLE [dbo].[User_FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_FacetTemplateInstance_CardOption1] FOREIGN KEY([DefaultCardOptionId])
REFERENCES [dbo].[CardOption] ([Id])
GO
ALTER TABLE [dbo].[User_FacetTemplateInstance] CHECK CONSTRAINT [FK_User_FacetTemplateInstance_CardOption1]
GO
ALTER TABLE [dbo].[User_FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_FacetTemplateInstance_FacetTemplateInstance] FOREIGN KEY([FacetTemplateInstanceId])
REFERENCES [dbo].[FacetTemplateInstance] ([Id])
GO
ALTER TABLE [dbo].[User_FacetTemplateInstance] CHECK CONSTRAINT [FK_User_FacetTemplateInstance_FacetTemplateInstance]
GO
ALTER TABLE [dbo].[User_FacetTemplateInstance]  WITH CHECK ADD  CONSTRAINT [FK_User_FacetTemplateInstance_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[User_FacetTemplateInstance] CHECK CONSTRAINT [FK_User_FacetTemplateInstance_User]
GO
ALTER TABLE [dbo].[Vote_CommentFacet]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentFacet_CommentFacet] FOREIGN KEY([CommentFacetId])
REFERENCES [dbo].[CommentFacet] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentFacet] CHECK CONSTRAINT [FK_Vote_CommentFacet_CommentFacet]
GO
ALTER TABLE [dbo].[Vote_CommentFacet]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentFacet_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentFacet] CHECK CONSTRAINT [FK_Vote_CommentFacet_User]
GO
ALTER TABLE [dbo].[Vote_CommentFacetTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentFacetTemplate_CommentFacetTemplate] FOREIGN KEY([CommentFacetTemplateId])
REFERENCES [dbo].[CommentFacetTemplate] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentFacetTemplate] CHECK CONSTRAINT [FK_Vote_CommentFacetTemplate_CommentFacetTemplate]
GO
ALTER TABLE [dbo].[Vote_CommentFacetTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_CommentFacetTemplate_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_CommentFacetTemplate] CHECK CONSTRAINT [FK_Vote_CommentFacetTemplate_User]
GO
ALTER TABLE [dbo].[Vote_Facet]  WITH CHECK ADD  CONSTRAINT [FK_Vote_Facet_Facet] FOREIGN KEY([FacetId])
REFERENCES [dbo].[Facet] ([Id])
GO
ALTER TABLE [dbo].[Vote_Facet] CHECK CONSTRAINT [FK_Vote_Facet_Facet]
GO
ALTER TABLE [dbo].[Vote_Facet]  WITH CHECK ADD  CONSTRAINT [FK_Vote_Facet_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_Facet] CHECK CONSTRAINT [FK_Vote_Facet_User]
GO
ALTER TABLE [dbo].[Vote_FacetTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_FacetTemplate_FacetTemplate] FOREIGN KEY([FacetTemplateId])
REFERENCES [dbo].[FacetTemplate] ([Id])
GO
ALTER TABLE [dbo].[Vote_FacetTemplate] CHECK CONSTRAINT [FK_Vote_FacetTemplate_FacetTemplate]
GO
ALTER TABLE [dbo].[Vote_FacetTemplate]  WITH CHECK ADD  CONSTRAINT [FK_Vote_FacetTemplate_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Vote_FacetTemplate] CHECK CONSTRAINT [FK_Vote_FacetTemplate_User]
GO
USE [master]
GO
ALTER DATABASE [CardOverflow] SET  READ_WRITE 
GO

