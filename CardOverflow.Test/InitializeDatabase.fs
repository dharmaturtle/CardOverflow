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
open System.Text
open FSharp.Control.Tasks
open System.Threading.Tasks
open Microsoft.SqlServer.Management.Smo
open Microsoft.SqlServer.Management.Common
    
let ankiModels = "{\"1554689669581\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646410, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [], \"id\": 1554689669581, \"req\": [[0, \"all\", [0]]]}, \"1554689669577\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646288, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (and reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669577, \"req\": [[0, \"all\", [0]], [1, \"all\", [1]]]}, \"1554689669572\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646292, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (optional reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Add Reverse\", \"ord\": 2, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{#Add Reverse}}{{Back}}{{/Add Reverse}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669572, \"req\": [[0, \"all\", [0]], [1, \"all\", [1, 2]]]}, \"1554689669571\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646306, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (type in the answer)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\\n{{type:Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669571, \"req\": [[0, \"all\", [0]]]}, \"1554689669570\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646315, \"usn\": -1, \"vers\": [], \"type\": 1, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n.nightMode .cloze {\\n color: lightblue;\\n}\", \"name\": \"Cloze\", \"flds\": [{\"name\": \"Text\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Extra\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Cloze\", \"ord\": 0, \"qfmt\": \"{{cloze:Text}}\", \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669570}}"

// This should be a function because each user needs to be a new instance. Otherwise, tests run in parallel by Ncrunch fail.
let deleteAndRecreateDatabase(db: CardOverflowDb) = task {
    db.Database.EnsureDeleted() |> ignore
    db.Database.EnsureCreated() |> ignore
    do! UserRepository.add db "Admin" "admin@cardoverflow.io"
    do! UserRepository.add db "The Collective" "theCollective@cardoverflow.io"
    do! UserRepository.add db "RoboTurtle" "roboturtle@cardoverflow.io"
    let theCollective = db.User.Include(fun x -> x.DefaultCardOption).Single(fun x -> x.DisplayName = "The Collective")
    let toEntity (cardTemplate: AnkiCardTemplateInstance) =
        cardTemplate.CopyToNew theCollective.Id <| theCollective.DefaultCardOption
    Anki.parseModels
        theCollective.Id
        ankiModels
    |> Result.getOk
    |> Seq.collect (snd >> Seq.map toEntity)
    |> db.CardTemplateInstance.AddRange
    do! db.SaveChangesAsyncI () }

//[<Fact>]
let ``Delete and Recreate localhost's CardOverflow Database via EF`` (): Task<unit> = task {
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    do! c.GetInstance<CardOverflowDb>() |> deleteAndRecreateDatabase }

let deleteAndRecreateDb dbName baseConnectionString =
    use conn = new SqlConnection(ConnectionString.value baseConnectionString)
    conn.Open()
    let server = conn |> ServerConnection |> Server
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
        File.ReadAllText @"..\netcoreapp3.1\Stuff\InitializeDatabase.sql"
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
    |> server.ConnectionContext.ExecuteNonQuery
    |> ignore
    conn.Close()

//[<Fact>]
let ``Delete and Recreate localhost's CardOverflow Database via SqlScript`` (): unit =
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    c.GetInstance<ConnectionString>() |> deleteAndRecreateDb "CardOverflow"
