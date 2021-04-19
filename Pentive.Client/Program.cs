using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pentive.Client {
  public class Program {
    public static async Task Main(string[] args) {
      var builder = WebAssemblyHostBuilder.CreateDefault(args);
      builder.RootComponents.Add<App>("#app");

      builder.Services.AddHttpClient("Pentive.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
          .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

      // Supply HttpClient instances that include access tokens when making requests to the server project
      builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Pentive.ServerAPI"));

      builder.Services.AddMsalAuthentication(options => {
        builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
        options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration["DefaultAccessTokenScope"]);
      });

      await builder.Build().RunAsync();
    }
  }
}
