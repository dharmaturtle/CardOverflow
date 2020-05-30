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
      var serilogLogger = ContainerExtensions.Logger.get(configuration);
      services.AddSingleton<Serilog.ILogger>(serilogLogger);
      var loggerFactory = new LoggerFactory(); // lowTODO figure out if we need to dispose
      services.AddDbContextPool<CardOverflowDb>(optionsBuilder => {
        //loggerFactory.AddSerilog(serilogLogger);
        optionsBuilder
        //.UseLoggerFactory(loggerFactory)
        //.EnableSensitiveDataLogging()
        .UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
      });
      Log.Logger = serilogLogger;
      services.AddLogging(x => x
        .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
      return services;
    }

  }
}
