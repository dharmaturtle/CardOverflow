$db = "CardOverflow"
$connectionString = "Host=localhost;Database=$db;Username=postgres;"

pg_dump -U postgres -p 5432 -d $db -f "InitializeDatabase.sql" -w --column-inserts

((((Get-Content -Raw InitializeDatabase.sql) -replace "--.*\n\n","") -replace "--.*\n", "") -replace "", "") -replace "", "" | Out-File -Encoding "UTF8BOM" InitializeDatabase.sql

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

# Remove-Item CardOverflow.Entity\* -Include *entity.cs
# Remove-Item CardOverflow.Entity\* -Include *CardOverflowDb.cs
# Remove-Item CardOverflow.Entity\* -Include *CardOverflowDbOverride.cs
# dotnet ef dbcontext scaffold $connectionString Npgsql.EntityFrameworkCore.PostgreSQL --context CardOverflowDb --force --project CardOverflow.Entity --data-annotations --use-database-names
# 
# foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
#     (Get-Content $file.PSPath) |
#     Foreach-Object { $_ -replace "InverseParent", "Children" } |
#     Set-Content $file.PSPath
# }
# 
# foreach ($file in Get-ChildItem -Path "CardOverflow.Entity\CardOverflowDb.cs") {
#     (Get-Content $file.PSPath -Raw) `
#     -replace [regex] "modelBuilder.Entity<CollectedCardEntity>", "base.OnModelCreating(modelBuilder);`r`n`r`n            modelBuilder.Entity<CollectedCardEntity>" `
#     -replace [regex] "entity.HasIndex\(e => e.DisplayName\)", "entity.ToTable(`"User`");`r`n`r`n                entity.HasIndex(e => e.DisplayName)" `
#     -replace [regex] '\s+\.HasFilter\("\(\[DisplayName\] IS NOT NULL\)"\)', '' `
#     -replace [regex] ".WithOne\(p => p.CardSetting\)", ".WithMany(p => p.CardSettings)" `
#     -replace [regex] ".HasForeignKey<CardSetting>\(d => d.UserId\)", ".HasForeignKey(d => d.UserId)" `
#     -replace [regex] "(?m)#warning.*?\n", "" `
#     -replace [regex] "optionsBuilder.Use.*", "throw new ArgumentOutOfRangeException();" |
#     Set-Content $file.PSPath
# }
# 
# foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
#     Replace-TextInFile $file.FullName "InversePrimaryChild" "InversePrimaryChild_UseParentInstead"
# }
# 
# foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
#     Replace-TextInFile $file.FullName "\[StringLength\((\d+)\)\]\s+public string (\S+) { get; set; }" '[StringLength($1)]
#         public string $2 {
#             get => _$2;
#             set {
#                 if (value.Length > $1) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and $2 has a maximum length of $1. Attempted value: {value}");
#                 _$2 = value;
#             }
#         }
#         private string _$2;'
# }
# 
# git -c diff.mnemonicprefix=false -c core.quotepath=false --no-optional-locks checkout CardOverflow.Entity/CardOverflowDbOverride.cs
# 
# Read-Host -Prompt “Press Enter to exit”
