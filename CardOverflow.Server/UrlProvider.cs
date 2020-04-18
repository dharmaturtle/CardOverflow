using Blazor.FileReader;
using Blazored.Toast;
using CardOverflow.Api;
using CardOverflow.Entity;
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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace CardOverflow.Server {

  public class UrlProvider {
    public UrlProvider(string serverSideBlazor, string identityProvider, string userContentApi) {
      ServerSideBlazor = serverSideBlazor;
      IdentityProvider = identityProvider;
      UserContentApi = userContentApi;
    }
    public string ServerSideBlazor { get; }
    public string IdentityProvider { get; }
    public string UserContentApi { get; }
  }
}
