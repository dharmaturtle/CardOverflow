using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ThoughtDesign.WebLibrary {

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
