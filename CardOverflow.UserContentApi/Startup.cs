using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardOverflow.Entity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CardOverflow.UserContentApi {
  public class Startup {
    public IWebHostEnvironment Environment { get; }
    public IConfiguration Configuration { get; }

    public Startup(IWebHostEnvironment environment, IConfiguration configuration) {
      Environment = environment;
      Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
      services.AddControllers();

      services.AddSingleton<IEntityHasher, ContainerExtensions.EntityHasher>();
      services.AddDbContextPool<CardOverflowDb>(optionsBuilder => optionsBuilder.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
      Serilog.Log.Logger = new LoggerConfiguration()
        .ReadFrom
        .Configuration(Configuration)
        .CreateLogger();
      services.AddLogging(x => x
        .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
      });
    }
  }
}
