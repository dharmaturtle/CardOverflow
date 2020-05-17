using CardOverflow.Pure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThoughtDesign.WebLibrary {
  public static class PureTools {

    public static int GetQueryInt(this NavigationManager navigationManager, string key, int fallbackValue = 0) =>
      navigationManager.Uri
        .Apply(navigationManager.ToAbsoluteUri).Query
        .Apply(QueryHelpers.ParseQuery)
        .TryGetValue(key, out var token) &&
        int.TryParse(token[0], out int possibleId)
        ? possibleId
        : fallbackValue;

    public static UrlProvider UrlProvider(this IConfiguration configuration) =>
      new UrlProvider(
        configuration.GetSection("BaseUrls:Server").Value,
        configuration.GetSection("BaseUrls:IdentityProvider").Value,
        configuration.GetSection("BaseUrls:UserContentApi").Value
      );

    public static ContentResult ToTextHtmlContent(this string s, ControllerBase controllerBase) =>
      controllerBase.Content(s, "text/html");

    public static IEnumerable<T> ZipLongest<T1, T2, T>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, T> operation) { // https://stackoverflow.com/a/44010411
      using var iter1 = first.GetEnumerator();
      using var iter2 = second.GetEnumerator();
      while (iter1.MoveNext()) {
        if (iter2.MoveNext()) {
          yield return operation(iter1.Current, iter2.Current);
        } else {
          yield return operation(iter1.Current, default);
        }
      }
      while (iter2.MoveNext()) {
        yield return operation(default, iter2.Current);
      }
    }

  }
}
