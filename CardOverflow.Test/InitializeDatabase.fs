module InitializeDatabase

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
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
open System.Text.RegularExpressions
open Npgsql
open System.Reflection
    
let ankiModels = "{\"1554689669581\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646410, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [], \"id\": 1554689669581, \"req\": [[0, \"all\", [0]]]}, \"1554689669577\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646288, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (and reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669577, \"req\": [[0, \"all\", [0]], [1, \"all\", [1]]]}, \"1554689669572\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646292, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (optional reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Add Reverse\", \"ord\": 2, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{#Add Reverse}}{{Back}}{{/Add Reverse}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669572, \"req\": [[0, \"all\", [0]], [1, \"all\", [1, 2]]]}, \"1554689669571\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646306, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (type in the answer)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\\n{{type:Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669571, \"req\": [[0, \"all\", [0]]]}, \"1554689669570\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646315, \"usn\": -1, \"vers\": [], \"type\": 1, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n.nightMode .cloze {\\n color: lightblue;\\n}\", \"name\": \"Cloze\", \"flds\": [{\"name\": \"Text\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Extra\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Cloze\", \"ord\": 0, \"qfmt\": \"{{cloze:Text}}\", \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669570}}"

// This should be a function because each user needs to be a new leaf. Otherwise, tests run in parallel by Ncrunch fail.
let deleteAndRecreateDatabase(db: CardOverflowDb) = task {
    db.Database.EnsureDeleted() |> ignore
    db.Database.EnsureCreated() |> ignore
    do! UserRepository.create db user_1 "Admin"
    do! UserRepository.create db user_2 "The Collective"
    do! UserRepository.create db user_3 "RoboTurtle"
    let theCollective = db.User.Include(fun x -> x.DefaultCardSetting).Single(fun x -> x.DisplayName = "The Collective")
    let toEntity (gromplate: AnkiGrompleaf) =
        gromplate.CopyToNew theCollective.Id <| theCollective.DefaultCardSetting
    Anki.parseModels
        theCollective.Id
        ankiModels
    |> Result.getOk
    |> Seq.map (snd >> toEntity)
    |> db.Grompleaf.AddRange
    do! db.SaveChangesAsyncI () }

//[<Fact>]
let ``Delete and Recreate localhost's CardOverflow Database via EF`` (): Task<unit> = task {
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    do! c.GetInstance<CardOverflowDb>() |> deleteAndRecreateDatabase }

let executeNonQuery command connectionString =
    use connection = new NpgsqlConnection(connectionString)
    connection.Open()
    use cmd = new NpgsqlCommand(command, connection)
    cmd.ExecuteNonQuery() |> ignore

let readResource (name: string) = // https://stackoverflow.com/a/3314213
    let assembly = Assembly.GetExecutingAssembly()
    // resource name format: {Namespace}.{Folder}.{Filename}.{Extension}
    use stream =
        assembly.GetManifestResourceNames().Single(fun s -> s.EndsWith name)
        |> assembly.GetManifestResourceStream
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

let fullReset databaseName serverConnectionString =
    let serverConnectionString = ConnectionString.value serverConnectionString
    executeNonQuery
        <|  """
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = 'CardOverflow'
              AND pid <> pg_backend_pid();
            DROP DATABASE IF EXISTS "CardOverflow";
            CREATE DATABASE "CardOverflow";
            """.Replace("CardOverflow", databaseName)
        <|  serverConnectionString
    let testDbConnectionString = sprintf "%s;Database=%s;" serverConnectionString databaseName
    executeNonQuery
        <| readResource "InitializeDatabase.sql"
        <| testDbConnectionString
    use connection = new NpgsqlConnection(testDbConnectionString)
    connection.Open()
    connection.ReloadTypes()

let tweak databaseName serverConnectionString =
    let serverConnectionString = ConnectionString.value serverConnectionString
    executeNonQuery
        <| readResource "TweakDevelopment.sql"
        <| sprintf "%s;Database=%s;" serverConnectionString databaseName

//[<Fact>]
let ``Delete and Recreate localhost's CardOverflow Database via SqlScript`` (): unit =
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterServerConnectionString
    c.GetInstance<ConnectionString>() |> fullReset "CardOverflow"
    c.GetInstance<ConnectionString>() |> tweak "CardOverflow"

let fastResetScript =
    let insertMasterData =
        Regex("""(^INSERT INTO.*?;.*?)ALTER""", RegexOptions.Singleline + RegexOptions.Multiline)
            .Match(File.ReadAllText @"..\netcoreapp3.1\Stuff\InitializeDatabase.sql")
            .Groups.[1].Value
    sprintf // https://stackoverflow.com/a/12082038
        """SET session_replication_role = 'replica';
        DO
        $func$
        BEGIN
           EXECUTE
           (SELECT 'TRUNCATE TABLE ' || string_agg(oid::regclass::text, ', ') || ' CASCADE'
            FROM   pg_class
            WHERE  relkind = 'r'  -- only tables
            AND    relnamespace = 'public'::regnamespace
           );
        END
        $func$;
        %s
        SET session_replication_role = 'origin';"""
        insertMasterData

let fastReset databaseName serverConnectionString =
    let serverConnectionString = ConnectionString.value serverConnectionString
    executeNonQuery fastResetScript
        <| sprintf "%s;Database=%s;" serverConnectionString databaseName
    
let databaseExists databaseName connectionString =
    use connection = new NpgsqlConnection(ConnectionString.value connectionString)
    use command = new NpgsqlCommand(sprintf "SELECT 1 FROM pg_database WHERE datname='%s'" databaseName, connection) // sql injection vector - do not use in prod https://stackoverflow.com/a/33782992
    connection.Open()
    command.ExecuteScalar() |> isNull |> not

let reset databaseName serverConnectionString =
    if databaseExists databaseName serverConnectionString then
        fastReset
    else
        fullReset
    <| databaseName
    <| serverConnectionString

//[<Fact>]
let ``Delete all rows and reinsert master data into localhost's CardOverflow Database`` (): unit =
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    c.GetInstance<ConnectionString>() |> reset "CardOverflow"

(*
select 'select pg_terminate_backend(pid) from pg_stat_activity where datname='''||datname||''';'
from pg_database
where datistemplate=false AND datname like 'Ω_%';
select 'drop database "'||datname||'";'
from pg_database
where datistemplate=false AND datname like 'Ω_%';
*)