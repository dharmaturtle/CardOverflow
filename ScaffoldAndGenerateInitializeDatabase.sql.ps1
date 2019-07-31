$connectionString = "Server=localhost;Database=CardOverflow;User Id=localsa;"

mssql-scripter --connection-string $connectionString --schema-and-data --file-path ./InitializeDatabase.sql
# If the above has problems, consider using --check-for-existence https://github.com/Microsoft/mssql-scripter
(((Get-Content -Raw InitializeDatabase.sql) -replace " +S[\w: \/]+\*{6}\/"," ******/") -replace " CONTAINMENT[^?]*?GO", "GO") -replace "\[varbinary\]\(32\)", "[binary](32)" | Out-File -Encoding "UTF8BOM" InitializeDatabase.sql

Remove-Item CardOverflow.Entity\* -Include *entity.cs
Remove-Item CardOverflow.Entity\* -Include *CardOverflowDb.cs
dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer --context CardOverflowDb --force --project CardOverflow.Entity --data-annotations

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
    (Get-Content $file.PSPath) |
    Foreach-Object { $_ -replace "InverseParent", "Children" } |
    Set-Content $file.PSPath
}

# https://github.com/aspnet/EntityFrameworkCore/issues/11298
foreach ($file in Get-ChildItem -Path "CardOverflow.Entity\UserEntity.cs") {
    (Get-Content $file.PSPath) `
    -replace "public virtual CardOptionEntity CardOption", "public virtual ICollection<CardOptionEntity> CardOptions" `
    -replace [regex] "HashSet<AcquiredCardEntity>\(\)\;", "HashSet<AcquiredCardEntity>();`r`n            CardOptions = new HashSet<CardOptionEntity>();" |
    Set-Content $file.PSPath
}
foreach ($file in Get-ChildItem -Path "CardOverflow.Entity\CardOptionEntity.cs") {
    ([regex] 'InverseProperty\("CardOption"\)').Replace((Get-Content $file.PSPath -Raw), 'InverseProperty("CardOptions")', 1) |
    Set-Content $file.PSPath
}

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity\CardOverflowDb.cs") {
    (Get-Content $file.PSPath -Raw) `
    -replace [regex] ".WithOne\(p => p.CardOption\)", ".WithMany(p => p.CardOptions)" `
    -replace [regex] ".HasForeignKey<CardOption>\(d => d.UserId\)", ".HasForeignKey(d => d.UserId)" `
    -replace [regex] "(?m)#warning.*?\n", "" `
    -replace [regex] "optionsBuilder.Use.*", "throw new ArgumentOutOfRangeException();" `
    -replace [regex] "entity.HasOne\(d => d.C\)", "entity.HasOne(d => d.Card)" `
    -replace [regex] "(?sm)modelBuilder\.HasAnnotation\(\`"ProductVersion.*?  +", "" |
    Set-Content $file.PSPath
}

function Replace-TextInFile {
    Param(
        [string]$FilePath,
        [string]$Pattern,
        [string]$Replacement
    )

    [System.IO.File]::WriteAllText(
        $FilePath,
        ([System.IO.File]::ReadAllText($FilePath) -replace $Pattern, $Replacement)
    )
}

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
    Replace-TextInFile $file.FullName "InversePrimaryChild" "InversePrimaryChild_UseParentInstead"
}

Replace-TextInFile (Get-Item "CardOverflow.Entity\AcquiredCardEntity.cs").FullName "public virtual CardEntity C" "public virtual CardEntity Card"
Replace-TextInFile (Get-Item "CardOverflow.Entity\CardEntity.cs").FullName 'InverseProperty\("C"\)' 'InverseProperty("Card")'

Read-Host -Prompt “Press Enter to exit”
