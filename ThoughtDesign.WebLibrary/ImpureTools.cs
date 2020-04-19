using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThoughtDesign.WebLibrary {
  public static class ImpureTools {

    public static IServiceCollection RegisterCommonStuff(this IServiceCollection services, IConfiguration configuration) {
      services.AddSingleton(configuration.UrlProvider());
      services.AddSingleton<IEntityHasher, ContainerExtensions.EntityHasher>();
      services.AddDbContextPool<CardOverflowDb>(optionsBuilder => optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
      Serilog.Log.Logger = new LoggerConfiguration()
        .ReadFrom
        .Configuration(configuration)
        .CreateLogger();
      services.AddLogging(x => x
        .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
      return services;
    }

  }
}
