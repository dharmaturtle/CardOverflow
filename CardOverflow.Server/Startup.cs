using Blazor.FileReader;
using Blazored.Toast;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Server.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.Extensions.Logging;
using DiffPlex;
using DiffPlex.DiffBuilder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using ThoughtDesign.WebLibrary;
using System;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using BlazorStrap;
using FluentValidation;
using CardOverflow.Server.Pages.Deck;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Fluxor;

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
      const string clientId = "cardoverflowserversideblazorclient";
      services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
      })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => {
          options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.Authority = Configuration.UrlProvider().IdentityProvider;
          options.ClientId = clientId;
          options.ClientSecret = Configuration.GetSection("ClientSecret:" + clientId).Value;
          options.ResponseType = OpenIdConnectResponseType.Code;
          options.UsePkce = true;
          options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
          options.Scope.Add("display_name"); // Ref: https://www.pluralsight.com/courses/asp-dotnet-core-oauth2-openid-connect-securing/ Securing ASP.NET Core with OAuth2 and OpenID Connect/Working with Claims in Your Web Application/Demo - Getting Ready for Calling the UserInfo Endpoint
          options.ClaimActions.MapUniqueJsonKey("display_name", "display_name");
          options.ClaimActions.DeleteClaims("sid", "idp", "s_hash", "auth_time", "amr");
          options.SaveTokens = true;
          options.GetClaimsFromUserInfoEndpoint = true;
        });
      services.AddScoped<ISideBySideDiffBuilder, SideBySideDiffBuilder>();
      services.AddScoped<IDiffer, Differ>();
      services.AddBlazoredToast();
      services.AddBootstrapCss();
      services.AddMvc();
      services.AddTransient<IValidator<FollowCommandViewModel>, FollowCommandViewModelValidator>();
      services.AddSingleton<RandomProvider>();
      services.AddSingleton<TimeProvider>();
      services.AddSingleton<Scheduler>();

      services.AddFileReaderService(options => options.InitializeOnFirstCall = true); // medTODO what does this do?
      services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddSingleton<WeatherForecastService>();
      services.AddHttpClient<UserContentHttpClient>();

      services.RegisterCommonStuff(Configuration);

      services.AddHsts(options => {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromMinutes(5); // highTODO https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-3.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts For production environments that are implementing HTTPS for the first time, set the initial HstsOptions.MaxAge to a small value using one of the TimeSpan methods. Set the value from hours to no more than a single day in case you need to revert the HTTPS infrastructure to HTTP. After you're confident in the sustainability of the HTTPS configuration, increase the HSTS max-age value; a commonly used value is one year.
      });

      var currentAssembly = typeof(Startup).Assembly;
      services.AddFluxor(options => options.ScanAssemblies(currentAssembly));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseDatabaseErrorPage();
      } else {
        app.UseExceptionHandler("/Error");
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
