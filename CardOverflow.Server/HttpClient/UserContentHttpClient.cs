using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.Server {
  public class UserContentHttpClient {
    private readonly HttpClient _client;

    public UserContentHttpClient(HttpClient httpClient) {
      httpClient.BaseAddress = new Uri("https://localhost:5011/");
      _client = httpClient;
    }

    public Task<string> GetFront(int cardId) =>
      _client.GetStringAsync("/card/rawfront/" + cardId);

  }
}
