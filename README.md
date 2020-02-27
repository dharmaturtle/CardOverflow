# CardOverflow

StackOverflow for flashcards.

Video demo:

[![Video demo](https://img.youtube.com/vi/OdNVhK1odA8/maxresdefault.jpg)](https://youtu.be/OdNVhK1odA8)

## Getting started

1. Create an empty database in SQL Server.
2. Duplicate `Config\appsettings.Local.json.template`, renaming it `appsettings.Local.json`. Update the connection string to point to the database you just created.
3. Run the test named `Delete and Recreate localhost's CardOverflow Database via SqlScript` in `CardOverflow.Test\InitializeDatabase.fs`.
4. Set `CardOverflow.Server` as the startup project and `F5` or run `CardOverflow.Server\dotnet-watch-run.ps1`.
5. Run `INSERT INTO [CardOverflow].[dbo].[AlphaBetaKey] VALUES (0,0)`. Then create an account using the web interface using `0` as the invite code.
6. Insert data from [here](https://ankiweb.net/shared/decks/) or `CardOverflow.Test\AnkiExports\...` by running the import: https://localhost:44315/import

## Database scaffolding

Run `ScaffoldAndGenerateInitializeDatabase.sql.ps1` to generate `InitializeDatabase.sql`.
This requires PowerShell Core 6.0 and [mssql-scripter](https://github.com/Microsoft/mssql-scripter).
