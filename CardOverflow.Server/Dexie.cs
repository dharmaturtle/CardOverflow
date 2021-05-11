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
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using static Domain.Projection;

namespace CardOverflow.Server {
  public class Dexie {
    private readonly IJSRuntime _jsRuntime;

    const string BULK_PUT_EVENTS = "bulkPutEvents";
    const string GET_ALL_UNSYNCED = "getAllUnsynced";
    const string GET_NEXT_QUIZ_CARD = "getNextQuizCard";

    const string DECK_PREFIX = "Deck";
    const string STACK_PREFIX = "Stack";
    const string DECK_STREAM = "DeckStream";
    const string STACK_STREAM = "StackStream";

    public Dexie(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task Append(IEnumerable<ClientEvent<Deck.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeDecks(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, DECK_PREFIX, eventsString, summaries);
    }

    public async Task Append(IEnumerable<ClientEvent<Stack.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeStacks(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, STACK_PREFIX, eventsString, summaries);
    }

    public async Task<(
      IList<ClientEvent<Deck.Events.Event>>,
      IList<ClientEvent<Stack.Events.Event>>
    )> GetAllUnsynced() {
      var events = await _jsRuntime.InvokeAsync<IList<JsonElement>>(GET_ALL_UNSYNCED);
      var deckEvents = events[0].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Deck.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var stackEvents = events[1].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Stack.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      return (deckEvents, stackEvents);
    }

    public async Task<FSharpOption<Summary.Card>> GetNextQuizCard() {
      var stackJson = await _jsRuntime.InvokeAsync<string>(GET_NEXT_QUIZ_CARD);
      return Projection.Dexie.parseNextQuizCard(stackJson);
    }

  }
}
