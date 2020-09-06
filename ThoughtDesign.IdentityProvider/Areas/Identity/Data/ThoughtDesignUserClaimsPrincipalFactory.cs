using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ThoughtDesign.IdentityProvider.Areas.Identity.Data {

  public class ThoughtDesignUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ThoughtDesignUser> {
    public ThoughtDesignUserClaimsPrincipalFactory(UserManager<ThoughtDesignUser> userManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager, optionsAccessor) {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ThoughtDesignUser user) {
      var identity = await base.GenerateClaimsAsync(user);
      identity.AddClaim(new Claim("display_name", user.DisplayName));
      return identity;
    }
  }
}
