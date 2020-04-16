using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
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
            options.UseSqlServer(
                context.Configuration.GetConnectionString("IdentityDbConnection")));
        services.AddIdentity<ThoughtDesignUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
          .AddEntityFrameworkStores<IdentityDb>()
          .AddDefaultTokenProviders();
      });
    }

  }
}
