// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace ThoughtDesign.IdentityProvider {
  public static class Config {
    public static IEnumerable<IdentityResource> Ids =>
      new IdentityResource[] {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("display_name", new List<string> { "display_name"})
      };

    public static IEnumerable<ApiResource> Apis =>
      new ApiResource[] { };

    public static IEnumerable<Client> Clients =>
      new Client[] {
        new Client {
          ClientId = "cardoverflowserversideblazorclient",
          AllowedGrantTypes = GrantTypes.Hybrid,
          ClientSecrets = { new Secret("tempclientsecret".Sha256()) },
          RedirectUris = { "https://localhost:44315/signin-oidc" },
          PostLogoutRedirectUris = { "https://localhost:44315/signout-callback-oidc" },
          RequireConsent = false,
          AllowOfflineAccess = true,
          AllowedScopes = {
            IdentityServerConstants.StandardScopes.OpenId,
            IdentityServerConstants.StandardScopes.Profile,
            "display_name"
          },
        },
      };

    public static void SeedDatabase(IApplicationBuilder app) {
      using var serviceScope = app.ApplicationServices
        .GetService<IServiceScopeFactory>().CreateScope();

      serviceScope.ServiceProvider
        .GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

      var context = serviceScope.ServiceProvider
        .GetRequiredService<ConfigurationDbContext>();
      context.Database.Migrate();
      if (!context.Clients.Any()) {
        foreach (var client in Config.Clients) {
          context.Clients.Add(client.ToEntity());
        }
        context.SaveChanges();
      }

      if (!context.IdentityResources.Any()) {
        foreach (var resource in Config.Ids) {
          context.IdentityResources.Add(resource.ToEntity());
        }
        context.SaveChanges();
      }

      if (!context.ApiResources.Any()) {
        foreach (var resource in Config.Apis) {
          context.ApiResources.Add(resource.ToEntity());
        }
        context.SaveChanges();
      }
    }

  }
}
