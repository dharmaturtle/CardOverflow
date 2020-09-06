using System;
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
      foreach (var entity in builder.Model.GetEntityTypes()) {
        entity
          .GetTableName()
          .TrimEnd('s')
          .Replace("AspNet", "")
          .Pipe(snakeCase.RewriteName)
          .Pipe(x => x == "user" ? "padawan" : x)
          .Do(entity.SetTableName);
        foreach (var key in entity.GetKeys()) {
          key
            .GetName()
            .TrimEnd('s')
            .Do(key.SetName);
        }
      }
    }
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
