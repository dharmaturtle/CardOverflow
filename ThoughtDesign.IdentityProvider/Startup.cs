// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;

namespace ThoughtDesign.IdentityProvider {
  public class Startup {
    public IWebHostEnvironment Environment { get; }

    public Startup(IWebHostEnvironment environment) {
      Environment = environment;
    }

    public void ConfigureServices(IServiceCollection services) {
      services.AddControllersWithViews();

      var builder = services.AddIdentityServer()
          .AddInMemoryIdentityResources(Config.Ids) // highTODO replace
          .AddInMemoryApiResources(Config.Apis)
          .AddInMemoryClients(Config.Clients)
          .AddAspNetIdentity<ThoughtDesignUser>();

      // not recommended for production - you need to store your key material somewhere secure highTODO
      builder.AddDeveloperSigningCredential();
    }

    public void Configure(IApplicationBuilder app) {
      if (Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }

      app.UseStaticFiles();
      app.UseRouting();

      app.UseIdentityServer();

      app.UseAuthorization();
      app.UseEndpoints(endpoints => {
        endpoints.MapDefaultControllerRoute();
      });
    }
  }
}
