using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardOverflow.Pure;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.Server {
  public class UserContentHttpClient {
    private readonly HttpClient _client;

    public UserContentHttpClient(HttpClient httpClient, UrlProvider urlProvider) {
      httpClient.BaseAddress = new Uri(urlProvider.UserContentApi);
      _client = httpClient;
    }

    public Task<string> GetStrippedFront(Guid stackId) =>
      _client.GetStringAsync("stack/" + stackId + "/front/")
        .ContinueWith(x => x.Result.Apply(MappingTools.stripHtmlTagsForDisplay));

  }
}
