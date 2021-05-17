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
using CardOverflow.Pure;

namespace CardOverflow.Server {
  public class Dexie {
    private readonly IJSRuntime _jsRuntime;

    const string BULK_PUT_EVENTS = "bulkPutEvents";
    const string BULK_PUT_SUMMARIES = "bulkPutSummaries";
    const string GET_ALL_UNSYNCED = "getAllUnsynced";
    const string GET_SUMMARY = "getSummary";
    const string GET_STREAM = "getStream";
    const string GET_NEXT_QUIZ_CARD = "getNextQuizCard";
    const string GET_VIEW_DECKS = "getViewDecks";

    const string USER_PREFIX = "User";
    const string DECK_PREFIX = "Deck";
    const string TEMPLATE_PREFIX = "Template";
    const string EXAMPLE_PREFIX = "Example";
    const string STACK_PREFIX = "Stack";
    
    const string CARD_SUMMARY = "CardSummary";
    
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

    public async Task Append(IEnumerable<ClientEvent<Domain.Example.Events.Event>> events) {
      var summaries = Projection.Dexie.summarizeExamples(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, EXAMPLE_PREFIX, eventsString, summaries);
    }

    public async Task Append(IEnumerable<ClientEvent<Stack.Events.Event>> events) {
      var (stacks, cards) = Projection.Dexie.summarizeStacksAndCards(events);
      var eventsString = Serdes.Serialize(events, jsonSerializerSettings);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, STACK_PREFIX, eventsString, stacks);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_SUMMARIES, CARD_SUMMARY, cards);
    }

    public async Task<(
      IList<ClientEvent<User.Events.Event>>,
      IList<ClientEvent<Deck.Events.Event>>,
      IList<ClientEvent<Template.Events.Event>>,
      IList<ClientEvent<Domain.Example.Events.Event>>,
      IList<ClientEvent<Stack.Events.Event>>
    )> GetAllUnsynced() {
      var events = await _jsRuntime.InvokeAsync<IList<JsonElement>>(GET_ALL_UNSYNCED);
      var userEvents = events[0].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<User.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var deckEvents = events[1].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Deck.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var templateEvents = events[2].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Template.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var exampleEvents = events[3].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Domain.Example.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      var stackEvents = events[4].EnumerateArray().Select(e => Serdes.Deserialize<ClientEvent<Stack.Events.Event>>(e.GetString(), jsonSerializerSettings)).ToList();
      return (userEvents, deckEvents, templateEvents, exampleEvents, stackEvents);
    }

    private async Task<TResult> _get<TResult>(string prefix, Guid id) {
      var json = await _jsRuntime.InvokeAsync<string>(GET_SUMMARY, prefix, id.ToString());
      return Serdes.Deserialize<TResult>(json, jsonSerializerSettings);
    }

    public Task<Summary.User> GetUser(Guid id) => _get<Summary.User>(USER_PREFIX, id);
    public Task<Summary.Deck> GetDeck(Guid id) => _get<Summary.Deck>(DECK_PREFIX, id);
    public Task<Summary.Template> GetTemplate(Guid id) => _get<Summary.Template>(TEMPLATE_PREFIX, id);
    public Task<Summary.Example> GetExample(Guid id) => _get<Summary.Example>(EXAMPLE_PREFIX, id);
    public Task<Summary.Stack> GetStack(Guid id) => _get<Summary.Stack>(STACK_PREFIX, id);

    private async Task<List<ClientEvent<TResult>>> _getStream<TResult>(string prefix, Guid id) {
      var jsons = await _jsRuntime.InvokeAsync<List<string>>(GET_STREAM, prefix, id.ToString());
      return jsons.Select(j => Serdes.Deserialize<ClientEvent<TResult>>(j, jsonSerializerSettings)).ToList();
    }

    public Task<List<ClientEvent<User.Events.Event>>> GetUserStream(Guid id) => _getStream<User.Events.Event>(USER_PREFIX, id);
    public Task<List<ClientEvent<Deck.Events.Event>>> GetDeckStream(Guid id) => _getStream<Deck.Events.Event>(DECK_PREFIX, id);
    public Task<List<ClientEvent<Template.Events.Event>>> GetTemplateStream(Guid id) => _getStream<Template.Events.Event>(TEMPLATE_PREFIX, id);
    public Task<List<ClientEvent<Domain.Example.Events.Event>>> GetExampleStream(Guid id) => _getStream<Domain.Example.Events.Event>(EXAMPLE_PREFIX, id);
    public Task<List<ClientEvent<Stack.Events.Event>>> GetStackStream(Guid id) => _getStream<Stack.Events.Event>(STACK_PREFIX, id);

    public async Task<User.Fold.State> GetUserState(Guid id) => (await GetUserStream(id)).Select(ce => ce.Event).Pipe(User.Fold.foldInit.Invoke);
    public async Task<Deck.Fold.State> GetDeckState(Guid id) => (await GetDeckStream(id)).Select(ce => ce.Event).Pipe(Deck.Fold.foldInit.Invoke);
    public async Task<Template.Fold.State> GetTemplateState(Guid id) => (await GetTemplateStream(id)).Select(ce => ce.Event).Pipe(Template.Fold.foldInit.Invoke);
    public async Task<Domain.Example.Fold.State> GetExampleState(Guid id) => (await GetExampleStream(id)).Select(ce => ce.Event).Pipe(Domain.Example.Fold.foldInit.Invoke);
    public async Task<Stack.Fold.State> GetStackState(Guid id) => (await GetStackStream(id)).Select(ce => ce.Event).Pipe(Stack.Fold.foldInit.Invoke);

    public async Task<FSharpOption<Tuple<Summary.Stack, Summary.Card>>> GetNextQuizCard() {
      var stackJson = await _jsRuntime.InvokeAsync<string>(GET_NEXT_QUIZ_CARD);
      return Projection.Dexie.parseNextQuizCard(stackJson);
    }

    public async Task<List<ViewDeck>> GetViewDecks(Guid defaultDeckId) {
      var elements = await _jsRuntime.InvokeAsync<List<JsonElement>>(GET_VIEW_DECKS);
      return elements.Select(e => {
        var dueCount = e.GetProperty("dueCount").GetInt32();
        var allCount = e.GetProperty("allCount").GetInt32();
        var summaryString = e.GetProperty("summary").GetString();
        var deck = Serdes.Deserialize<Summary.Deck>(summaryString, jsonSerializerSettings);
        return Projection.Dexie.toViewDeck(deck, allCount, dueCount, defaultDeckId);
      }).ToList();
    }

  }
}
