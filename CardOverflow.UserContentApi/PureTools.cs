using CardOverflow.Pure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.UserContentApi {
  public static class PureTools {

    public static ContentResult ToTextHtmlContent(this string s, ControllerBase controllerBase) =>
      controllerBase.Content(s, "text/html");

  }
}
