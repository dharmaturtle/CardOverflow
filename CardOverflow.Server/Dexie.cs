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
    const string GET_ALL_UNSYNCED = "getAllUnsynced";

    const string DECK_PREFIX = "Deck";
    const string STACK_PREFIX = "Stack";
    const string DECK_STREAM = "DeckStream";
    const string STACK_STREAM = "StackStream";

    public Dexie(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task Append(IEnumerable<ClientEvent<Deck.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeDecks(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      var summariesString = Serdes.Serialize(summaries, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, DECK_PREFIX, eventsString, summariesString);
    }

    public async Task Append(IEnumerable<ClientEvent<Stack.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeStacks(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      var summariesString = Serdes.Serialize(summaries, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, STACK_PREFIX, eventsString, summariesString);
    }

    public async Task<(
      IList<ClientEvent<Deck.Events.Event>>,
      IList<ClientEvent<Stack.Events.Event>>
    )> GetAllUnsynced() {
      var events = (await _jsRuntime.InvokeAsync<JsonElement>(GET_ALL_UNSYNCED)).EnumerateArray().ToList();
      var deckEvents = events[0].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Deck.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var stackEvents = events[1].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Stack.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      return (deckEvents, stackEvents);
    }

  }
}
