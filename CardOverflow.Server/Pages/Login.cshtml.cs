using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardOverflow.Server.Pages {
  public class LoginModel : PageModel {
    
    public async Task OnGetAsync() {
      if (HttpContext.User.Identity.IsAuthenticated) {
        Response.Redirect(Url.Content("~/"));
      } else {
        await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
      }
    }

  }
}
