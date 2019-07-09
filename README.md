Run `dotnet watch run` in `\CardOverflow.Web`. It's beautiful.

Scaffold-DbContext "Server=localhost;Database=CardOverflow;User Id=localsa;" Microsoft.EntityFrameworkCore.SqlServer -Context CardOverflowDb -Force -DataAnnotations

Scaffold-DbContext "DataSource=C:\path\to\collection.anki2" Microsoft.EntityFrameworkCore.Sqlite -Context AnkiDb -Force -DataAnnotations -OutputDir Anki

Run `GenerateInitializeDatabase.sql.ps1` to generate `InitializeDatabase.sql`.
This requires PowerShell Core 6.0 and mssql-scripter https://github.com/Microsoft/mssql-scripter
