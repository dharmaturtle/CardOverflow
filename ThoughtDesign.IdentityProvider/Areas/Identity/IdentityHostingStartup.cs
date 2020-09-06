using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;
using ThoughtDesign.IdentityProvider.Data;

[assembly: HostingStartup(typeof(ThoughtDesign.IdentityProvider.Areas.Identity.IdentityHostingStartup))]
namespace ThoughtDesign.IdentityProvider.Areas.Identity {
  public class IdentityHostingStartup : IHostingStartup {

    public void Configure(IWebHostBuilder builder) {
      builder.ConfigureServices((context, services) => {
        services.AddDbContextPool<IdentityDb>(options =>
            options
              .UseSnakeCaseNamingConvention()
              .UseNpgsql(context.Configuration.GetConnectionString("IdentityDbConnection")));
        services.AddIdentity<ThoughtDesignUser, IdentityRole<Guid>>(options => {
          if (context.HostingEnvironment.IsDevelopment()) {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 1;
            options.Password.RequireNonAlphanumeric = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
          } else {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = true;
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedAccount = true;
          }
        }).AddEntityFrameworkStores<IdentityDb>()
          .AddClaimsPrincipalFactory<ThoughtDesignUserClaimsPrincipalFactory>()
          .AddDefaultTokenProviders();
        services.AddSingleton<IEmailSender, EmailSender>();
      });
    }

  }
}
