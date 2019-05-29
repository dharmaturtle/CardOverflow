module InitializeDatabase

open CardOverflow.Api
open CardOverflow.Entity
open ContainerExtensions
open System
open Xunit
open SimpleInjector
open System.Data.SqlClient
open System.IO
open System.Linq

let importedDate = DateTime(2020, 1, 1)

let defaultCardOptions =
    { Id = 1
      Name = "Default"
      NewCardsSteps = [ TimeSpan.FromMinutes 1.0; TimeSpan.FromMinutes 10.0 ]
      NewCardsMaxPerDay = int16 20
      NewCardsGraduatingInterval = TimeSpan.FromDays 1.0
      NewCardsEasyInterval = TimeSpan.FromDays 4.0
      NewCardsStartingEaseFactor = 2.5
      NewCardsBuryRelated = true
      MatureCardsMaxPerDay = int16 200
      MatureCardsEaseFactorEasyBonusFactor = 1.3
      MatureCardsIntervalFactor = 1.0
      MatureCardsMaximumInterval = TimeSpan.FromDays 36500.0
      MatureCardsHardInterval = 1.2
      MatureCardsBuryRelated = true
      LapsedCardsSteps = [ TimeSpan.FromMinutes 10.0 ]
      LapsedCardsNewIntervalFactor = 0.0
      LapsedCardsMinimumInterval = TimeSpan.FromDays 1.0
      LapsedCardsLeechThreshold = byte 8
      ShowAnswerTimer = false
      AutomaticallyPlayAudio = false
      ReplayQuestionAudioOnAnswer = false
      AnkiId = None }
let defaultAnkiCardOptions =
    { Id = 2
      Name = "Default Anki Options"
      NewCardsSteps = [ TimeSpan.FromMinutes 1.0; TimeSpan.FromMinutes 10.0 ]
      NewCardsMaxPerDay = int16 20
      NewCardsGraduatingInterval = TimeSpan.FromDays 1.0
      NewCardsEasyInterval = TimeSpan.FromDays 4.0
      NewCardsStartingEaseFactor = 2.5
      NewCardsBuryRelated = false
      MatureCardsMaxPerDay = int16 200
      MatureCardsEaseFactorEasyBonusFactor = 1.3
      MatureCardsIntervalFactor = 1.0
      MatureCardsMaximumInterval = TimeSpan.FromDays 36500.0
      MatureCardsHardInterval = 1.2
      MatureCardsBuryRelated = false
      LapsedCardsSteps = [ TimeSpan.FromMinutes 10.0 ]
      LapsedCardsNewIntervalFactor = 0.0
      LapsedCardsMinimumInterval = TimeSpan.FromDays 1.0
      LapsedCardsLeechThreshold = byte 8
      ShowAnswerTimer = false
      AutomaticallyPlayAudio = false
      ReplayQuestionAudioOnAnswer = false
      AnkiId = None }

let frontField =
    { Name = "Front"
      Ordinal = byte 0
      Font = "Arial"
      FontSize = byte 20
      IsRightToLeft = false
      IsSticky = false }
let basicFrontBackCardTemplate =
    { Name = "Card Template"
      Ordinal = byte 0
      QuestionTemplate = "{{Front}}"
      AnswerTemplate = "{{FrontSide}}\n\n<hr id=answer>\n\n{{Back}}"
      ShortQuestionTemplate = ""
      ShortAnswerTemplate = "" }
let basicConceptTemplate =
    { Id = 0
      Name = "Basic"
      Css = ".card {
    font-family: arial;
    font-size: 20px;
    text-align: center;
    color: black;
    background-color: white;
}"
      Fields = 
          [ frontField
            { frontField with
                Name = "Back"
                Ordinal = byte 1 }]
      CardTemplates = [ basicFrontBackCardTemplate ]
      Modified = importedDate
      IsCloze = false
      DefaultPublicTags = []
      DefaultPrivateTags = []
      DefaultCardOptionId = 1
      LatexPre = @"\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}"
      LatexPost = @"\end{document}" }
let basicWithReversedCardConceptTemplate =
    { basicConceptTemplate with
        Name = "Basic with reversed card"
        CardTemplates = [
            basicFrontBackCardTemplate
            { basicFrontBackCardTemplate with
                Name = "Reversed Card Template"
                Ordinal = byte 1
                QuestionTemplate = "{{Back}}"
                AnswerTemplate = "{{FrontSide}}\n\n<hr id=answer>\n\n{{Front}}" }]}
let basicWithOptionalReversedCardConceptTemplate =
    { basicWithReversedCardConceptTemplate with
        Name = "Basic with optional reversed card"
        Fields = 
            [ frontField
              { frontField with
                  Name = "Back"
                  Ordinal = byte 1 }
              { frontField with
                  Name = "Leave Nonempty to Generate Reversed Card"
                  Ordinal = byte 2 }]
        CardTemplates = 
            [ basicFrontBackCardTemplate
              { basicFrontBackCardTemplate with
                  Name = "Optional Reversed Card Template"
                  Ordinal = byte 1
                  QuestionTemplate = "{{#Leave Nonempty to Generate Reversed Card}}{{Back}}{{/Leave Nonempty to Generate Reversed Card}}"
                  AnswerTemplate = "{{FrontSide}}\n\n<hr id=answer>\n\n{{Front}}" }]}
let basicTypeInAnswerConceptTemplate =
    { basicConceptTemplate with
        Name = "Basic type in the answer"
        CardTemplates = 
            [{ basicFrontBackCardTemplate with
                 QuestionTemplate = "{{Front}}\n{{type:Back}}"
                 AnswerTemplate = "{{FrontSide}}\n\n<hr id=answer>\n\n{{Back}}" }]}
let basicClozeConceptTemplate =
    { basicConceptTemplate with
        Name = "Basic Cloze"
        IsCloze = true
        Css = ".card {
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
}"
        Fields =
            [{ frontField with
                 Name = "Text" }
             { frontField with
                 Name = "Extra"
                 Ordinal = byte 1 }]
        CardTemplates = 
            [{ basicFrontBackCardTemplate with
                 Name = "Basic Cloze"
                 QuestionTemplate = "{{cloze:Text}}"
                 AnswerTemplate = "{{cloze:Text}}<br>\n{{Extra}}" }]}

// This should be a function because each user needs to be a new instance. Otherwise, tests run in parallel by Ncrunch fail.
let deleteAndRecreateDatabase(dbService: IDbService) =
    let admin = UserEntity(DisplayName = "Admin", Email = "admin@cardoverflow.io")
    let theCollective = UserEntity(DisplayName = "The Collective", Email = "theCollective@cardoverflow.io")
    let roboturtle = UserEntity(DisplayName = "RoboTurtle", Email = "roboturtle@cardoverflow.io")
    dbService.Command(fun db ->
        db.Database.EnsureDeleted() |> ignore
        db.Database.EnsureCreated() |> ignore
        db.Users.AddRange
            [ admin
              theCollective
              roboturtle ]
        db.CardOptions.AddRange
            [ defaultCardOptions.CopyToNew theCollective
              defaultAnkiCardOptions.CopyToNew theCollective ]
        db.ConceptTemplates.AddRange 
            [ basicConceptTemplate.CopyToNew theCollective
              basicWithReversedCardConceptTemplate.CopyToNew theCollective
              basicWithOptionalReversedCardConceptTemplate.CopyToNew theCollective
              basicTypeInAnswerConceptTemplate.CopyToNew theCollective
              basicClozeConceptTemplate.CopyToNew theCollective ]
    )

//[<Fact>]
let ``Delete and Recreate localhost's CardOverflow Database via EF``() =
    use c = new Container()
    c.RegisterStuff
    c.RegisterStandardConnectionString
    c.GetInstance<IDbService>() |> deleteAndRecreateDatabase

let deleteAndRecreateDb dbName =
    let conn = new SqlConnection "Server=localhost;Trusted_Connection=True;"
    conn.Open()
    [
        """
        USE [master]
        GO
        IF EXISTS (SELECT name FROM sys.databases WHERE name = N'CardOverflow')
        BEGIN
            ALTER DATABASE [CardOverflow] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
            DROP DATABASE [CardOverflow]
        END
        GO
        """
        File.ReadAllText @"..\netcoreapp3.0\Stuff\InitializeDatabase.sql"
    ]
    |> String.concat "\r\n"
    |> fun s -> s.Replace("[CardOverflow]", sprintf "[%s]" dbName)
                 .Replace("'CardOverflow'", sprintf "'%s'" dbName)
                 .Split("GO").Where(fun x -> x.Any())
    |> Seq.iter(fun s -> 
        use command = new SqlCommand(s,conn)
        command.ExecuteNonQuery() |> ignore
    )
    conn.Close()

//[<Fact>]
let ``Delete and Recreate localhost's CardOverflow Database via SqlScript``() =
    deleteAndRecreateDb "CardOverflow"
