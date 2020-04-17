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
        services.AddDbContext<IdentityDb>(options =>
            options.UseNpgsql(
                context.Configuration.GetConnectionString("IdentityDbConnection")));
        services.AddIdentity<ThoughtDesignUser, IdentityRole<int>>(options => {
          options.User.RequireUniqueEmail = true;
          options.Password.RequireDigit = true;
          options.Password.RequireLowercase = true;
          options.Password.RequireUppercase = true;
          options.Password.RequiredLength = 6;
          options.Password.RequireNonAlphanumeric = true;
          //options.SignIn.RequireConfirmedEmail = true; // highTODO
          //options.SignIn.RequireConfirmedAccount = true; // highTODO
        }).AddEntityFrameworkStores<IdentityDb>()
          .AddDefaultTokenProviders();
        services.AddSingleton<IEmailSender, EmailSender>();
      });
    }

  }
}
