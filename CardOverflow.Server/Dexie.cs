using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FsCodec.NewtonsoftJson;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.JSInterop;
using Domain;
using System.Linq;
using static Domain.Infrastructure;
using CardOverflow.Legacy;
using NodaTime;
using CardOverflow.Debug;
using System.Text.Json;
using static Domain.Projection;

namespace CardOverflow.Server {
  public class Dexie {
    private readonly IJSRuntime _jsRuntime;

    const string BULK_PUT_EVENTS = "bulkPutEvents";
    const string GET_UNSYNCED_EVENTS = "getUnsyncedEvents";

    const string DECK_PREFIX = "Deck";
    const string DECK_STREAM = "DeckStream";

    public Dexie(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task Append(IEnumerable<ClientEvent<Deck.Events.Event>> events) {
      var summaries = events.GroupBy(x => x.StreamId).ToDictionary(
        e => e.Key,
        e => Deck.Fold.fold.Invoke(Deck.Fold.initial).Invoke(e.Select(x => x.Event)));
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      var summariesString = Serdes.Serialize(summaries, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, DECK_PREFIX, eventsString, summariesString);
    }

    public async Task<IList<ClientEvent<Deck.Events.Event>>> GetUnsyncedDeckEvents() {
      var events = await _jsRuntime.InvokeAsync<JsonElement>(GET_UNSYNCED_EVENTS, DECK_STREAM);
      return events.EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Deck.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
    }

  }
}
