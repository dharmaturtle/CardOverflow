$connectionString = "Server=localhost;Database=CardOverflow;Trusted_Connection=True;"

mssql-scripter --connection-string $connectionString --schema-and-data --file-path ./InitializeDatabase.sql
# If the above has problems, consider using --check-for-existence https://github.com/Microsoft/mssql-scripter
((Get-Content -Raw InitializeDatabase.sql) -replace " +S[\w: \/]+\*{6}\/"," ******/") -replace " CONTAINMENT[^?]*?GO", "GO" | Out-File -Encoding "UTF8BOM" InitializeDatabase.sql

dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer --context CardOverflowDb --force --project CardOverflow.Entity --data-annotations

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
    (Get-Content $file.PSPath) |
    Foreach-Object { $_ -replace "InverseParent", "Children" } |
    Set-Content $file.PSPath
}

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity\AcquiredCardEntity.cs") {
    (Get-Content $file.PSPath) |
    Foreach-Object { $_ -replace "byte MemorizationStateAndCardState", "MemorizationStateAndCardStateEnum MemorizationStateAndCardState" } |
    Set-Content $file.PSPath
}

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity\CardOverflowDb.cs") {
    (Get-Content $file.PSPath -Raw) `
    -replace [regex] "(?m)#warning.*?\n", "" `
    -replace [regex] "optionsBuilder.Use.*", "throw new ArgumentOutOfRangeException();" `
    -replace [regex] "(?sm)modelBuilder\.HasAnnotation\(\`"ProductVersion.*?  +", "" |
    Set-Content $file.PSPath
}

Read-Host -Prompt “Press Enter to exit”