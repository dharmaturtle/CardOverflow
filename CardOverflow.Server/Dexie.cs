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

namespace CardOverflow.Server {
  public class Dexie {
    private readonly IJSRuntime _jsRuntime;
    
    const string BULK_PUT_EVENTS = "bulkPutEvents";
    const string GET_UNSYNCED_EVENTS = "getUnsyncedEvents";

    const string DECK = "Deck";

    public Dexie(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task Append(IEnumerable<Deck.Events.Event> events) {
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, DECK, eventsString);
    }

    public async Task<IList<Deck.Events.Event>> GetUnsyncedDeckEvents() {
      var events = await _jsRuntime.InvokeAsync<JsonElement>(GET_UNSYNCED_EVENTS, DECK);
      return events.EnumerateArray().Select(e => Serdes.Deserialize<Deck.Events.Event>(e.GetString(), jsonSerializerSettings)).ToList();
    }

  }
}
