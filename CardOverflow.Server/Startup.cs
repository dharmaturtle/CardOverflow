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

namespace CardOverflow.Server {
  public class Startup {

    public Startup() =>
      Configuration =
        ContainerExtensions.Configuration.get(ContainerExtensions.Environment.get);

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
      services.AddBlazoredToast();
      services.AddMvc();
      services.AddSingleton<RandomProvider>();
      services.AddSingleton<TimeProvider>();
      services.AddSingleton<Scheduler>();
      services.AddSingleton(
        new DbContextOptionsBuilder<CardOverflowDb>()
          .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
          .Options
        );
      services.AddSingleton<DbExecutor>();
      services.AddDbContext<CardOverflowDb>();
      services.AddDefaultIdentity<UserEntity>(options => {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireNonAlphanumeric = false;
        //options.SignIn.RequireConfirmedEmail = true; // medTODO
      }).AddEntityFrameworkStores<CardOverflowDb>();
      services.AddFileReaderService(options => options.InitializeOnFirstCall = true); // medTODO what does this do?
      services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UserEntity>>();
      services.AddSingleton<WeatherForecastService>();
      services.AddHttpClient<UserContentHttpClient>();

      Log.Logger = new LoggerConfiguration()
        .ReadFrom
        .Configuration(Configuration)
        .CreateLogger();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      app.UseRouting();

      if (env.IsDevelopment()) {
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
