# CardOverflow

StackOverflow for flashcards.

Video demo:

[![Video demo](https://img.youtube.com/vi/OdNVhK1odA8/maxresdefault.jpg)](https://youtu.be/OdNVhK1odA8)

## Getting started

1. Create two new databases in PostGreSQL (one each for CardOverflow and the IdentityProvider).
2. Duplicate `Config\appsettings.Development.json`, renaming it `Config\appsettings.Local.json`. Update the connection strings to point to the databases you just created.
3. Run the test named `Delete and Recreate localhost's CardOverflow Database via SqlScript` in `CardOverflow.Test\InitializeDatabase.fs`.
4. Set all projects in the `Endpoints` folder as the startup projects and `F5` or run `dotnet-watch-run.ps1`.
5. Run `INSERT INTO public."AlphaBetaKey" VALUES(1, 'key', 'f')`. Register an account using the web interface using `key` as the invite code.
6. Insert data from [here](https://ankiweb.net/shared/decks/) or `CardOverflow.Test\AnkiExports\...` by running the import: https://localhost:44315/import

## Database scaffolding

Run `ScaffoldAndGenerateInitializeDatabase.sql.ps1` to generate `InitializeDatabase.sql`.
