$connectionString = "Server=localhost;Database=CardOverflow;User Id=localsa;"

mssql-scripter --connection-string $connectionString --schema-and-data --file-path ./InitializeDatabase.sql
# If the above has problems, consider using --check-for-existence https://github.com/Microsoft/mssql-scripter
((((Get-Content -Raw InitializeDatabase.sql) -replace " +S[\w: \/]+\*{6}\/"," ******/") -replace " CONTAINMENT[^?]*?GO", "GO") -replace "\[varbinary\]\(32\)", "[binary](32)") -replace "WHERE \(\[ClozeIndex\] IS NOT NULL\)", "" | Out-File -Encoding "UTF8BOM" InitializeDatabase.sql

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

git checkout 2d8fb8279152cbaee0697136c605b5f34969965e -- "CardOverflow.Entity\CardOverflow.Entity.csproj" # resets the entity proj back to August 2nd 2019
git reset HEAD "CardOverflow.Entity\CardOverflow.Entity.csproj"

Remove-Item CardOverflow.Entity\* -Include *entity.cs
Remove-Item CardOverflow.Entity\* -Include *CardOverflowDb.cs
dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer --context CardOverflowDb --force --project CardOverflow.Entity --data-annotations --use-database-names
git checkout -- "CardOverflow.Entity\CardOverflow.Entity.csproj"

Remove-Item CardOverflow.Entity\* -Include AspNet*entity.cs

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
    -replace [regex] '\s+.HasFilter\("\(\[ClozeIndex\] IS NOT NULL\)"\)', "" `
    -replace [regex] "(?sm)using Microsoft.EntityFrameworkCore.Metadata;", "using Microsoft.EntityFrameworkCore.Metadata;`r`nusing Microsoft.AspNetCore.Identity;`r`nusing Microsoft.AspNetCore.Identity.EntityFrameworkCore;" `
    -replace [regex] "public partial class CardOverflowDb : DbContext", "public partial class CardOverflowDb : IdentityDbContext<UserEntity, IdentityRole<int>, int>" `
    -replace [regex] "(?sm)\s+public virtual DbSet<AspNetRoleClaimsEntity>.*?AspNetUserTokens.*?\}", "" `
    -replace [regex] "modelBuilder.Entity<AcquiredCardEntity>", "base.OnModelCreating(modelBuilder);`r`n`r`n            modelBuilder.Entity<AcquiredCardEntity>" `
    -replace [regex] "(?sm)modelBuilder.Entity<AspNetRoleClaimsEntity>.*?e\.Name\W+", "" `
    -replace [regex] "entity.HasIndex\(e => e.DisplayName\)", "entity.ToTable(`"User`");`r`n`r`n                entity.HasIndex(e => e.DisplayName)" `
    -replace [regex] '\s+\.HasFilter\("\(\[DisplayName\] IS NOT NULL\)"\)', '' `
    -replace [regex] '(?sm)\s+.HasFilter\("\(\[Email\] IS NOT NULL\)"\);.*?NULL\S+', ";" `
    -replace [regex] ".WithOne\(p => p.CardOption\)", ".WithMany(p => p.CardOptions)" `
    -replace [regex] ".HasForeignKey<CardOption>\(d => d.UserId\)", ".HasForeignKey(d => d.UserId)" `
    -replace [regex] "(?m)#warning.*?\n", "" `
    -replace [regex] "optionsBuilder.Use.*", "throw new ArgumentOutOfRangeException();" `
    -replace [regex] "(?sm)modelBuilder\.HasAnnotation\(\`"ProductVersion.*?  +", "" |
    Set-Content $file.PSPath
}

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
    Replace-TextInFile $file.FullName "InversePrimaryChild" "InversePrimaryChild_UseParentInstead"
}

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity\UserEntity.cs") {
    (Get-Content $file.PSPath -Raw) `
    -replace [regex] "public partial class UserEntity", "public partial class UserEntity : IdentityUser<int>" `
    -replace [regex] "using System.ComponentModel.DataAnnotations.Schema;", "using System.ComponentModel.DataAnnotations.Schema;`r`nusing Microsoft.AspNetCore.Identity;" `
    -replace [regex] "(?sm)\s+AspNetUserClaims =.*AspNetUserTokensEntity>\(\);", "" `
    -replace [regex] "(?sm)public int Id.*AccessFailedCount { get; set; }", "//[Required] // medTODO make this not nullable" `
    -replace [regex] "(?sm)\s+\S+\W+public virtual ICollection<AspNetUserClaimsEntity> AspNetUserClaims.*?AspNetUserTokens .*?\}", "" `
    -replace [regex] "(?sm)modelBuilder\.HasAnnotation\(\`"ProductVersion.*?  +", "" |
    Set-Content $file.PSPath
}

foreach ($file in Get-ChildItem -Path "CardOverflow.Entity" *.cs) {
    Replace-TextInFile $file.FullName "\[StringLength\((\d+)\)\]\s+public string (\S+) { get; set; }" '[StringLength($1)]
        public string $2 {
            get => _$2;
            set {
                if (value.Length > $1) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and $2 has a maximum length of $1. Attempted value: {value}");
                _$2 = value;
            }
        }
        private string _$2;'
}

Read-Host -Prompt “Press Enter to exit”
