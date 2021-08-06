using CardOverflow.Entity;
using CardOverflow.Pure;
using Dapper.NodaTime;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThoughtDesign.WebLibrary {
  public static class ImpureTools {

    public static IServiceCollection RegisterCommonStuff(this IServiceCollection services, IConfiguration configuration) {
      DapperNodaTimeSetup.Register();
      services.AddSingleton(configuration.UrlProvider());
      var serilogLogger = ContainerExtensions.Logger.get(configuration);
      services.AddSingleton<Serilog.ILogger>(serilogLogger);
      var loggerFactory = new LoggerFactory(); // lowTODO figure out if we need to dispose
      services.AddDbContextPool<CardOverflowDb>(optionsBuilder => {
        //loggerFactory.AddSerilog(serilogLogger);
        optionsBuilder
        //.UseLoggerFactory(loggerFactory)
        //.EnableSensitiveDataLogging()
        .UseSnakeCaseNamingConvention()
        .UseNpgsql(configuration.GetConnectionString("DefaultConnection"), x => x.UseNodaTime());
      });
      services.AddSingleton<DbExecutor>();
      services.AddSingleton<Func<Task<NpgsqlConnection>>>(_ => async () => {
        var conn = new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        return conn;
      });
      Log.Logger = serilogLogger;
      services.AddLogging(x => x
        .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
      return services;
    }

  }
}
