using CardOverflow.Pure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.Server {
  // Class members must be Pure! https://en.wikipedia.org/wiki/Pure_function
  public static class Tools {
    public static int GetQueryInt(this NavigationManager navigationManager, string key, int fallbackValue = 0) =>
      navigationManager.Uri
        .Apply(navigationManager.ToAbsoluteUri).Query
        .Apply(QueryHelpers.ParseQuery)
        .TryGetValue(key, out var token) &&
        int.TryParse(token[0], out int possibleId)
        ? possibleId
        : fallbackValue;
  }
}
