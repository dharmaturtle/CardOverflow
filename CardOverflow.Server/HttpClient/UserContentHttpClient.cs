using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardOverflow.Pure;

namespace CardOverflow.Server {
  public class UserContentHttpClient {
    private readonly HttpClient _client;

    public UserContentHttpClient(HttpClient httpClient) {
      httpClient.BaseAddress = new Uri("https://localhost:5011/");
      _client = httpClient;
    }

    public Task<string> GetStrippedFront(int cardId) =>
      _client.GetStringAsync("/card/" + cardId + "/front/")
        .ContinueWith(x => x.Result.Apply(MappingTools.stripHtmlTags));

  }
}
