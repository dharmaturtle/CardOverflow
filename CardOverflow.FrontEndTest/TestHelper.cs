using Blazored.Toast;
using BlazorStrap;
using Bunit;
using CardOverflow.Api;
using CardOverflow.Debug;
using CardOverflow.Entity;
using CardOverflow.Server;
using CardOverflow.Server.Pages.Deck;
using FluentValidation;
using FsToolkit.ErrorHandling;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ThoughtDesign.WebLibrary;
using Xunit;

namespace CardOverflow.FrontEndTest {
  public static class TestHelper {
    
    public static T SideEffect<T>(T input, Action<T> action) {
      action(input);
      return input;
    }

    public static bool Not(bool input) => !input;

    public static void Setup(TestServiceProvider services, Action<CardOverflowDb> setupDb) {
      if (!services.IsProviderInitialized) {
        services.AddBlazoredToast();
        services.AddScoped<NavigationManager, MockNavigationManager>();
        services.AddBootstrapCss();
        services.AddTransient<IValidator<FollowCommandViewModel>, FollowCommandViewModelValidator>();
        services.AddSingleton<DbExecutor>();
        services.AddSingleton<TimeProvider>();
        services.AddSingleton<Scheduler>();
        services.AddSingleton<RandomProvider>();
        services.AddDbContextPool<CardOverflowDb>(x => x.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<Func<Task<NpgsqlConnection>>>(_ => async () => {
          var conn = new NpgsqlConnection("Host=localhost;Database=CardOverflow;Username=postgres;");
          await conn.OpenAsync();
          return conn;
        });
      }

      using var databaseContext = services.GetService<CardOverflowDb>();
      databaseContext.Database.EnsureDeleted();
      databaseContext.Database.EnsureCreated();
      setupDb(databaseContext);
      databaseContext.SaveChanges();
      foreach (var entity in databaseContext.ChangeTracker.Entries()) {
        entity.State = EntityState.Detached;
      }
    }

  }

  // temp example code; this entire class may be deleted. If you do, also delete Microsoft.AspNetCore.TestHost from the csproj
  //public class PrimeWebDefaultRequestShould {
  //  private readonly TestServer _server;
  //  private readonly HttpClient _client;

  //  public PrimeWebDefaultRequestShould() {
  //    // Arrange

  //    //Host.CreateDefaultBuilder(args)
  //    //      .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())

  //    _server = new TestServer(new WebHostBuilder()
  //       .UseStartup<Startup>());
  //    _client = _server.CreateClient();
  //  }

  //  [Fact]
  //  public async System.Threading.Tasks.Task ReturnHelloWorld() {
  //    //Act
  //    var f = _server.Host.Services.GetService<Func<Task<NpgsqlConnection>>>();
  //    var ex = _server.Host.Services.GetService<DbExecutor>();
  //    //var db = _server.Host.Services.GetService<CardOverflowDb>();

  //    var ffasdf = await ex.QueryAsync(x => x.User.ToListAsync());
      
  //    var conn = await f();

  //    var asdf = await HistoryRepository.getHeatmap(conn, Guid.NewGuid());
  //    asdf.D();
  //   //var response = await _client.GetAsync("/terp");
  //   // response.EnsureSuccessStatusCode();
  //   // var responseString = await response.Content.ReadAsStringAsync();
  //   // // Assert
  //   // Assert.Equal("Hello World!", responseString);
  //  }
  //}
}
