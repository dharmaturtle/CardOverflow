using Blazor.FileReader;
using Blazored.Toast;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Server.Areas.Identity;
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
using Westwind.AspNetCore.LiveReload;

namespace CardOverflow.Server {
  public class Startup {
    private readonly IWebHostEnvironment _env;

    public Startup(IWebHostEnvironment env) {
      Configuration = ContainerExtensions.Configuration.get(ContainerExtensions.Environment.get);
      _env = env;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
      services.AddBlazoredToast();
      services.AddMvc();
      services.AddSingleton<RandomProvider>();
      services.AddSingleton<TimeProvider>();
      services.AddSingleton<Scheduler>();
      DbContextOptionsBuilder buildOptions(DbContextOptionsBuilder builder) =>
        builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
      services.AddSingleton(buildOptions(new DbContextOptionsBuilder<CardOverflowDb>()).Options);
      services.AddSingleton<DbExecutor>();
      services.AddSingleton<IEntityHasher, ContainerExtensions.EntityHasher>();
      services.AddEntityFrameworkSqlServer();
      services.AddDbContextPool<CardOverflowDb>(optionsBuilder => buildOptions(optionsBuilder));
      services.AddDefaultIdentity<UserEntity>(options => {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        //options.SignIn.RequireConfirmedEmail = true; // highTODO
      }).AddEntityFrameworkStores<CardOverflowDb>();
      services.AddFileReaderService(options => options.InitializeOnFirstCall = true); // medTODO what does this do?
      services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UserEntity>>();
      services.AddSingleton<WeatherForecastService>();
      services.AddHttpClient<UserContentHttpClient>();
      if (_env.IsDevelopment()) {
        services.AddLiveReload(config => {
          config.LiveReloadEnabled = true;
          config.ClientFileExtensions = ".css,.js,.htm,.html";
          config.FolderToMonitor = "~/../";
        });
      }

      Log.Logger = new LoggerConfiguration()
        .ReadFrom
        .Configuration(Configuration)
        .CreateLogger();
      services.AddLogging(x => x
        .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
      );
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app) {
      app.UseRouting();

      if (_env.IsDevelopment()) {
        app.UseLiveReload();
        app.UseDeveloperExceptionPage();
        app.UseDatabaseErrorPage();
      } else {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseMiddleware<ExceptionLoggingMiddleware>();

      app.UseHttpsRedirection();
      app.UseStaticFiles();

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
