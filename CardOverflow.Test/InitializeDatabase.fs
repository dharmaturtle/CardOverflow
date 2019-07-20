module InitializeDatabase

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open ContainerExtensions
open System
open Xunit
open SimpleInjector
open System.Data.SqlClient
open System.IO
open System.Linq
open SimpleInjector.Lifestyles
open Microsoft.EntityFrameworkCore

let importedDate = DateTime(2020, 1, 1)

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
      MaintainerId = 0
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
let deleteAndRecreateDatabase(db: CardOverflowDb) =
    db.Database.EnsureDeleted() |> ignore
    db.Database.EnsureCreated() |> ignore
    UserRepository.add db "Admin" "admin@cardoverflow.io"
    UserRepository.add db "The Collective" "theCollective@cardoverflow.io"
    UserRepository.add db "RoboTurtle" "roboturtle@cardoverflow.io"
    let theCollective = db.Users.AsNoTracking().Include(fun x -> x.CardOptions).First(fun x -> x.DisplayName = "The Collective")
    [ basicConceptTemplate
      basicWithReversedCardConceptTemplate
      basicWithOptionalReversedCardConceptTemplate
      basicTypeInAnswerConceptTemplate
      basicClozeConceptTemplate ]
    |> List.map (fun x -> { x with MaintainerId = theCollective.Id }.CopyToNew <| theCollective.CardOptions.First())
    |> db.ConceptTemplates.AddRange
    db.SaveChangesI ()

//[<Fact>]
let ``Delete and Recreate localhost's CardOverflow Database via EF``() =
    use c = new Container()
    c.RegisterStuff
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    c.GetInstance<CardOverflowDb>() |> deleteAndRecreateDatabase

let deleteAndRecreateDb dbName baseConnectionString =
    let conn = new SqlConnection(ConnectionString.value baseConnectionString)
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
        // https://stackoverflow.com/questions/25845836/could-not-obtain-information-about-windows-nt-group-user
        """
        USE [CardOverflow]
        GO 
        ALTER DATABASE [CardOverflow] set TRUSTWORTHY ON; 
        GO 
        EXEC dbo.sp_changedbowner @loginame = N'sa', @map = false 
        GO 
        sp_configure 'show advanced options', 1; 
        GO 
        RECONFIGURE; 
        GO 
        sp_configure 'clr enabled', 1; 
        GO 
        RECONFIGURE; 
        GO
        """
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
    use c = new Container()
    c.RegisterStuff
    c.RegisterStandardConnectionString
    c.GetInstance<ConnectionString>() |> deleteAndRecreateDb "CardOverflow"
