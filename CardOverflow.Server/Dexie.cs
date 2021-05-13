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

    const string USER_PREFIX = "User";
    const string DECK_PREFIX = "Deck";
    const string TEMPLATE_PREFIX = "Template";
    const string EXAMPLE_PREFIX = "Example";
    const string STACK_PREFIX = "Stack";
    
    const string USER_STREAM = "UserStream";
    const string DECK_STREAM = "DeckStream";
    const string TEMPLATE_STREAM = "TemplateStream";
    const string EXAMPLE_STREAM = "ExampleStream";
    const string STACK_STREAM = "StackStream";

    public Dexie(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task Append(IEnumerable<ClientEvent<User.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeUsers(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, USER_PREFIX, eventsString, summaries);
    }

    public async Task Append(IEnumerable<ClientEvent<Deck.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeDecks(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, DECK_PREFIX, eventsString, summaries);
    }

    public async Task Append(IEnumerable<ClientEvent<Template.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeTemplates(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, TEMPLATE_PREFIX, eventsString, summaries);
    }

    public async Task Append(IEnumerable<ClientEvent<Example.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeExamples(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, EXAMPLE_PREFIX, eventsString, summaries);
    }

    public async Task Append(IEnumerable<ClientEvent<Stack.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeStacks(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, STACK_PREFIX, eventsString, summaries);
    }

    public async Task<(
      IList<ClientEvent<User.Events.Event>>,
      IList<ClientEvent<Deck.Events.Event>>,
      IList<ClientEvent<Template.Events.Event>>,
      IList<ClientEvent<Example.Events.Event>>,
      IList<ClientEvent<Stack.Events.Event>>
    )> GetAllUnsynced() {
      var events = await _jsRuntime.InvokeAsync<IList<JsonElement>>(GET_ALL_UNSYNCED);
      var userEvents = events[0].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<User.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var deckEvents = events[1].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Deck.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var templateEvents = events[2].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Template.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var exampleEvents = events[3].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Example.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var stackEvents = events[4].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Stack.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      return (userEvents, deckEvents, templateEvents, exampleEvents, stackEvents);
    }

    public async Task<FSharpOption<Tuple<Summary.Stack, Summary.Card>>> GetNextQuizCard() {
      var stackJson = await _jsRuntime.InvokeAsync<string>(GET_NEXT_QUIZ_CARD);
      return Projection.Dexie.parseNextQuizCard(stackJson);
    }

  }
}
