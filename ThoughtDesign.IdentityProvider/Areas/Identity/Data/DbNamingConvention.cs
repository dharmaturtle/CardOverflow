﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardOverflow.Entity.DesignTime;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;

namespace ThoughtDesign.IdentityProvider.Data {

  // https://medium.com/@aspram.shadyan.dev/identityserver4-ef-core-naming-conventions-adapted-for-postgresql-29a138bd26bb
  public static class DbNamingConventionExtensions {
    public static void CustomizeNames(this ModelBuilder builder) {
      var snakeCase = new SnakeCaseNameRewriter(CultureInfo.CurrentCulture);
      builder.Model.GetEntityTypes().ToList().ForEach(entity => entity
        .GetTableName()
        .TrimEnd('s')
        .Replace("AspNet", "")
        .Pipe(snakeCase.RewriteName)
        .Pipe(toPadawan)
        .Pipe(toProperty)
        .Pipe(toIdp)
        .Do(entity.SetTableName));

      foreach (var entity in builder.Model.GetEntityTypes()) {
        var tableName = entity.GetTableName().Pipe(toUser);
        entity.GetKeys().ToList().ForEach(key => key.SetName($"{tableName}_pkey"));
        foreach (var fk in entity.GetForeignKeys()) {
          var otherTable = fk.PrincipalEntityType.GetTableName().Pipe(toUser);
          var c = fk.Properties[0].GetColumnName();
          fk.SetConstraintName($"{tableName} FK {otherTable}. {c}");
        }
        foreach (var ix in entity.GetIndexes()) {
          var columns = ix.Properties.Select(x => x.GetColumnName()).Pipe(x => System.String.Join(",", x));
          var uq = ix.IsUnique ? "uq" : "";
          ix.SetName($"{tableName}. {columns}. {uq}ix");
        }
      }
    }

    private static string toPadawan(string tableName) => tableName == "user" ? "padawan" : tableName;
    private static string toUser(string tableName) => tableName == "padawan" ? "user" : tableName;
    private static string toProperty(string tableName) => tableName.Replace("propertie", "property");
    private static string toIdp(string tableName) => tableName.Replace("_id_p_", "_idp_");
  }

  // literal copy paste of https://github.com/efcore/EFCore.NamingConventions/blob/master/EFCore.NamingConventions/NamingConventions/Internal/SnakeCaseNameRewriter.cs at commit 290cc330292d60bd1bad8eb28b46ef755de4b0cb
  public class SnakeCaseNameRewriter {
    private readonly CultureInfo _culture;

    public SnakeCaseNameRewriter(CultureInfo culture) => _culture = culture;

    public string RewriteName(string name) {
      if (string.IsNullOrEmpty(name))
        return name;

      var builder = new StringBuilder(name.Length + Math.Min(2, name.Length / 5));
      var previousCategory = default(UnicodeCategory?);

      for (var currentIndex = 0; currentIndex < name.Length; currentIndex++) {
        var currentChar = name[currentIndex];
        if (currentChar == '_') {
          builder.Append('_');
          previousCategory = null;
          continue;
        }

        var currentCategory = char.GetUnicodeCategory(currentChar);
        switch (currentCategory) {
          case UnicodeCategory.UppercaseLetter:
          case UnicodeCategory.TitlecaseLetter:
            if (previousCategory == UnicodeCategory.SpaceSeparator ||
                previousCategory == UnicodeCategory.LowercaseLetter ||
                previousCategory != UnicodeCategory.DecimalDigitNumber &&
                previousCategory != null &&
                currentIndex > 0 &&
                currentIndex + 1 < name.Length &&
                char.IsLower(name[currentIndex + 1])) {
              builder.Append('_');
            }

            currentChar = char.ToLower(currentChar, _culture);
            break;

          case UnicodeCategory.LowercaseLetter:
          case UnicodeCategory.DecimalDigitNumber:
            if (previousCategory == UnicodeCategory.SpaceSeparator)
              builder.Append('_');
            break;

          default:
            if (previousCategory != null)
              previousCategory = UnicodeCategory.SpaceSeparator;
            continue;
        }

        builder.Append(currentChar);
        previousCategory = currentCategory;
      }

      return builder.ToString();
    }
  }
}
