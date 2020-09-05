// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using CardOverflow.Entity;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using ThoughtDesign.IdentityProvider.Areas.Identity;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;
using ThoughtDesign.WebLibrary;

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

      services.RegisterCommonStuff(Configuration);
      
      var builder = services
        .AddIdentityServer()
        .AddAspNetIdentity<ThoughtDesignUser>();

      //if (Environment.IsDevelopment()) {
      //  builder
      //    .AddDeveloperSigningCredential()
      //    .AddInMemoryIdentityResources(Config.Ids)
      //    .AddInMemoryApiResources(Config.Apis)
      //    .AddInMemoryClients(Config.Clients);
      //} else {
        var assemblyName = typeof(IdentityHostingStartup).GetTypeInfo().Assembly.GetName().Name;
        var identityDbConnection = Configuration.GetConnectionString("IdentityDbConnection");
        void dbOptionsBuilder(DbContextOptionsBuilder builder) =>
          builder.UseNpgsql(identityDbConnection, options => options.MigrationsAssembly(assemblyName));
        builder
          .AddSigningCredential(_LoadCertificateFromStore())
          .AddConfigurationStore(options => options.ConfigureDbContext = dbOptionsBuilder)
          .AddOperationalStore(options => options.ConfigureDbContext = dbOptionsBuilder);
      //}
    }

    public void Configure(IApplicationBuilder app) {
      if (Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      } else {
        Config.SeedDatabase(app); // medTODO remove once mature in production
      }

      app.UseMiddleware<ExceptionLoggingMiddleware>();

      app.UseHttpsRedirection();
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
