Run `dotnet watch run` in `\CardOverflow.Web`. It's beautiful.

Scaffold-DbContext "Server=localhost;Database=CardOverflow;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -Context CardOverflowDb -Force -DataAnnotations

Scaffold-DbContext "DataSource=C:\path\to\collection.anki2" Microsoft.EntityFrameworkCore.Sqlite -Context AnkiDb -Force -DataAnnotations -OutputDir Anki