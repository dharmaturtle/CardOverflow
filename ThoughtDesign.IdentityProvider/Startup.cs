﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using CardOverflow.Entity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Security.Cryptography.X509Certificates;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;

namespace ThoughtDesign.IdentityProvider {
  public class Startup {
    public IWebHostEnvironment Environment { get; }
    public IConfiguration Configuration { get; }

    public Startup(IWebHostEnvironment environment, IConfiguration configuration) {
      Environment = environment;
      Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services) {
      services.AddMvc();

      services.AddDbContextPool<CardOverflowDb>(optionsBuilder => optionsBuilder.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
      services.AddSingleton<IEntityHasher, ContainerExtensions.EntityHasher>();

      var builder = services.AddIdentityServer()
          .AddInMemoryIdentityResources(Config.Ids) // highTODO replace
          .AddInMemoryApiResources(Config.Apis)
          .AddInMemoryClients(Config.Clients)
          .AddAspNetIdentity<ThoughtDesignUser>();

      if (Environment.IsDevelopment()) {
        builder.AddDeveloperSigningCredential();
      } else {
        builder.AddSigningCredential(_LoadCertificateFromStore());
      }
    }

    public void Configure(IApplicationBuilder app) {
      if (Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }

      app.UseStaticFiles();
      app.UseRouting();

      app.UseIdentityServer();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
        endpoints.MapDefaultControllerRoute();
        endpoints.MapRazorPages();
      });
    }

    private X509Certificate2 _LoadCertificateFromStore() {
      const string thumbPrint = "3eda15be748458261633f08b5b627f178e4e32a9"; // highTODO buy a real cert from a CSA https://app.pluralsight.com/course-player?clipId=d5db5c32-4eb2-49a9-aa1e-ef91603fb0ef
      using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
      store.Open(OpenFlags.ReadOnly);
      var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, true);
      if (certCollection.Count == 0) {
        throw new Exception("The specified certificate wasn't found.");
      }
      return certCollection[0];
    }

  }
}
