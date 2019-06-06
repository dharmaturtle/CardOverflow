$connectionString = "Server=localhost;Database=CardOverflow;Trusted_Connection=True;"

mssql-scripter --connection-string $connectionString --schema-and-data --file-path ./InitializeDatabase.sql
# If the above has problems, consider using --check-for-existence https://github.com/Microsoft/mssql-scripter
((Get-Content -Raw InitializeDatabase.sql) -replace " +S[\w: \/]+\*{6}\/"," ******/") -replace " CONTAINMENT[^?]*?GO", "GO" | Out-File -Encoding "UTF8BOM" InitializeDatabase.sql

dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer --context CardOverflowDb --force --project CardOverflow.Entity --data-annotations

Read-Host -Prompt “Press Enter to exit”