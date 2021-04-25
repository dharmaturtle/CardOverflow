using Tewr.Blazor.FileReader;
using Blazored.Toast;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.Extensions.Logging;
using DiffPlex;
using DiffPlex.DiffBuilder;
using Microsoft.AspNetCore.Authentication.Cookies;
using ThoughtDesign.WebLibrary;
using System.IdentityModel.Tokens.Jwt;
using BlazorStrap;
using FluentValidation;
using CardOverflow.Server.Pages.Deck;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CardOverflow.Server {
  public class Startup {
    public IWebHostEnvironment Environment { get; }
    public IConfiguration Configuration { get; }

    public Startup(IWebHostEnvironment environment, IConfiguration configuration) {
      Environment = environment;
      Configuration = configuration;
      JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
      services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
          .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"));
      services.AddControllersWithViews()
          .AddMicrosoftIdentityUI();

      services.AddAuthorization(options => {
        // By default, all incoming requests will be authorized according to the default policy
        //options.FallbackPolicy = options.DefaultPolicy;
      });
      services.AddScoped<ISideBySideDiffBuilder, SideBySideDiffBuilder>();
      services.AddScoped<IDiffer, Differ>();
      services.AddBlazoredToast();
      services.AddBootstrapCss();
      services.AddTransient<IValidator<FollowCommandViewModel>, FollowCommandViewModelValidator>();
      services.AddSingleton<RandomProvider>();
      services.AddSingleton<TimeProvider>();
      services.AddSingleton<Scheduler>();
      services.AddSingleton<NoCQS.User>();

      services.AddFileReaderService(options => options.InitializeOnFirstCall = true); // medTODO what does this do?
      services.AddRazorPages();
      services.AddServerSideBlazor()
          .AddMicrosoftIdentityConsentHandler();
      services.AddSingleton<WeatherForecastService>();
      services.AddHttpClient<UserContentHttpClient>();

      services.RegisterCommonStuff(Configuration);

      services.AddHsts(options => {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromMinutes(5); // highTODO https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-3.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts For production environments that are implementing HTTPS for the first time, set the initial HstsOptions.MaxAge to a small value using one of the TimeSpan methods. Set the value from hours to no more than a single day in case you need to revert the HTTPS infrastructure to HTTP. After you're confident in the sustainability of the HTTPS configuration, increase the HSTS max-age value; a commonly used value is one year.
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      } else {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseMiddleware<ExceptionLoggingMiddleware>();

      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
        endpoints.MapBlazorHub();
        endpoints.MapFallbackToPage("/_Host");
      });
    }

  }
}
