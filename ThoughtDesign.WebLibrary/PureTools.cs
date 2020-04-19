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

  }
}
