// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace ThoughtDesign.IdentityProvider {
  public static class Config {
    public static IEnumerable<IdentityResource> Ids =>
      new IdentityResource[] {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
      };

    public static IEnumerable<ApiResource> Apis =>
      new ApiResource[] { };

    public static IEnumerable<Client> Clients =>
      new Client[] {
        new Client {
          ClientId = "cardoverflowserversideblazorclient",
          AllowedGrantTypes = GrantTypes.Hybrid,
          ClientSecrets = { new Secret("secret".Sha256()) }, // highTODO
          RedirectUris = { "https://localhost:44315/signin-oidc" },
          PostLogoutRedirectUris = { "https://localhost:44315/signout-callback-oidc" },
          AllowOfflineAccess = true,
          AllowedScopes = {
            IdentityServerConstants.StandardScopes.OpenId,
            IdentityServerConstants.StandardScopes.Profile
          },
        },
      };

  }
}
