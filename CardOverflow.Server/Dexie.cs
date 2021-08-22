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
    const string GET_UNSYNCED = "getUnsynced";
    const string GET_SUMMARY = "getSummary";
    const string GET_SUMMARIES = "getSummaries";
    const string GET_ALL_SUMMARIES = "getAllSummaries";
    const string GET_STREAM = "getStream";
    const string GET_NEXT_QUIZ_CARD = "getNextQuizCard";
    const string GET_VIEW_DECKS = "getViewDecks";

    const string USER_PREFIX = "User";
    const string DECK_PREFIX = "Deck";
    const string TEMPLATE_PREFIX = "Template";
    const string EXAMPLE_PREFIX = "Example";
    const string STACK_PREFIX = "Stack";
    
    const string CARD_PREFIX = "Card";
    
    const string USER_STREAM = "UserStream";
    const string DECK_STREAM = "DeckStream";
    const string TEMPLATE_STREAM = "TemplateStream";
    const string EXAMPLE_STREAM = "ExampleStream";
    const string STACK_STREAM = "StackStream";

    public Dexie(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task _append<TEvent>(
      IEnumerable<ClientEvent<TEvent>> newEvents,
      Func<List<Guid>, Task<List<ClientEvent<TEvent>>>> getStream,
      Func<IEnumerable<ClientEvent<TEvent>>, IEnumerable<FSharpMap<string, string>>> summarize,
      string prefix) {
      var oldEvents = await getStream(newEvents.Select(x => x.StreamId).Distinct().ToList());
      var summaries = summarize(oldEvents.Concat(newEvents));
      var newEventsString = serializeToJson(newEvents);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, prefix, newEventsString, summaries);
    }

    public async Task Append(IEnumerable<ClientEvent<User.Events.Event>> newEvents) =>
      await _append(newEvents, GetUserStream, Projection.Dexie.summarizeUsers, USER_PREFIX);

    public async Task Append(IEnumerable<ClientEvent<PrivateDeck.Events.Event>> newEvents) =>
      await _append(newEvents, GetDeckStream, Projection.Dexie.summarizeDecks, DECK_PREFIX);

    public async Task Append(IEnumerable<ClientEvent<PublicTemplate.Events.Event>> newEvents) =>
      await _append(newEvents, GetTemplateStream, Projection.Dexie.summarizeTemplates, TEMPLATE_PREFIX);

    public async Task Append(IEnumerable<ClientEvent<Domain.Example.Events.Event>> newEvents) =>
      await _append(newEvents, GetExampleStream, Projection.Dexie.summarizeExamples, EXAMPLE_PREFIX);

    public async Task Append(IEnumerable<ClientEvent<Stack.Events.Event>> newEvents) {
      var oldEvents = await GetStackStream(newEvents.Select(x => x.StreamId).Distinct().ToList());
      var (stacks, cards) = await Projection.Dexie.summarizeStacksAndCards(oldEvents.Concat(newEvents), GetExampleInstance);
      var newEventsString = serializeToJson(newEvents);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_EVENTS, STACK_PREFIX, newEventsString, stacks);
      await _jsRuntime.InvokeVoidAsync(BULK_PUT_SUMMARIES, CARD_PREFIX, cards);
    }

    private async Task<List<ClientEvent<TResult>>> _getUnsynced<TResult>(string stream) =>
      (await _jsRuntime.InvokeAsync<List<string>>(GET_UNSYNCED, stream))
        .Pipe(_deserializeClientEvents<TResult>);

    public Task<List<ClientEvent<User.Events.Event>>> GetUserUnsynced() => _getUnsynced<User.Events.Event>(USER_STREAM);
    public Task<List<ClientEvent<PrivateDeck.Events.Event>>> GetDeckUnsynced() => _getUnsynced<PrivateDeck.Events.Event>(DECK_STREAM);
    public Task<List<ClientEvent<PublicTemplate.Events.Event>>> GetTemplateUnsynced() => _getUnsynced<PublicTemplate.Events.Event>(TEMPLATE_STREAM);
    public Task<List<ClientEvent<Domain.Example.Events.Event>>> GetExampleUnsynced() => _getUnsynced<Domain.Example.Events.Event>(EXAMPLE_STREAM);
    public Task<List<ClientEvent<Stack.Events.Event>>> GetStackUnsynced() => _getUnsynced<Stack.Events.Event>(STACK_STREAM);

    public async Task<(
      List<ClientEvent<User.Events.Event>>,
      List<ClientEvent<PrivateDeck.Events.Event>>,
      List<ClientEvent<PublicTemplate.Events.Event>>,
      List<ClientEvent<Domain.Example.Events.Event>>,
      List<ClientEvent<Stack.Events.Event>>
    )> GetAllUnsynced() {
      var events = await _jsRuntime.InvokeAsync<List<List<string>>>(GET_ALL_UNSYNCED);
      var userEvents = events[0].Pipe(_deserializeClientEvents<User.Events.Event>);
      var deckEvents = events[1].Pipe(_deserializeClientEvents<PrivateDeck.Events.Event>);
      var templateEvents = events[2].Pipe(_deserializeClientEvents<PublicTemplate.Events.Event>);
      var exampleEvents = events[3].Pipe(_deserializeClientEvents<Domain.Example.Events.Event>);
      var stackEvents = events[4].Pipe(_deserializeClientEvents<Stack.Events.Event>);
      return (userEvents, deckEvents, templateEvents, exampleEvents, stackEvents);
    }

    private async Task<TResult> _get<TResult>(string prefix, Guid id) {
      var json = await _jsRuntime.InvokeAsync<string>(GET_SUMMARY, prefix, id.ToString());
      return deserializeFromJson<TResult>(json);
    }

    public Task<Summary.User> GetUser(Guid id) => _get<Summary.User>(USER_PREFIX, id);
    public Task<Summary.PrivateDeck> GetDeck(Guid id) => _get<Summary.PrivateDeck>(DECK_PREFIX, id);
    public Task<Summary.PublicTemplate> GetTemplate(Guid id) => _get<Summary.PublicTemplate>(TEMPLATE_PREFIX, id);
    public Task<Summary.Example> GetExample(Guid id) => _get<Summary.Example>(EXAMPLE_PREFIX, id);
    public Task<Summary.Stack> GetStack(Guid id) => _get<Summary.Stack>(STACK_PREFIX, id);

    private async Task<List<TResult>> _get<TResult>(string prefix, IEnumerable<Guid> ids) {
      var jsons = await _jsRuntime.InvokeAsync<List<string>>(GET_SUMMARIES, prefix, ids);
      return jsons.Select(deserializeFromJson<TResult>).ToList();
    }

    public Task<List<Summary.User>> GetUser(IEnumerable<Guid> ids) => _get<Summary.User>(USER_PREFIX, ids);
    public Task<List<Summary.PrivateDeck>> GetDeck(IEnumerable<Guid> ids) => _get<Summary.PrivateDeck>(DECK_PREFIX, ids);
    public Task<List<Summary.PublicTemplate>> GetTemplate(IEnumerable<Guid> ids) => _get<Summary.PublicTemplate>(TEMPLATE_PREFIX, ids);
    public Task<List<Summary.Example>> GetExample(IEnumerable<Guid> ids) => _get<Summary.Example>(EXAMPLE_PREFIX, ids);
    public Task<List<Summary.Stack>> GetStack(IEnumerable<Guid> ids) => _get<Summary.Stack>(STACK_PREFIX, ids);

    private async Task<List<TResult>> _get<TResult>(string prefix) {
      var jsons = await _jsRuntime.InvokeAsync<List<string>>(GET_ALL_SUMMARIES, prefix);
      return jsons.Select(deserializeFromJson<TResult>).ToList();
    }

    public Task<List<Summary.User>> GetUsers() => _get<Summary.User>(USER_PREFIX);
    public Task<List<Summary.PrivateDeck>> GetDecks() => _get<Summary.PrivateDeck>(DECK_PREFIX);
    public Task<List<Summary.PublicTemplate>> GetTemplates() => _get<Summary.PublicTemplate>(TEMPLATE_PREFIX);
    public Task<List<Summary.Example>> GetExamples() => _get<Summary.Example>(EXAMPLE_PREFIX);
    public Task<List<Summary.Stack>> GetStacks() => _get<Summary.Stack>(STACK_PREFIX);
    public Task<List<Projection.Dexie.CardInstance>> GetCards() => _get<Projection.Dexie.CardInstance>(CARD_PREFIX);
    
    public async Task<TemplateInstance> GetTemplateInstance(Guid templateId, int ordinal) {
      var template = await GetTemplate(templateId);
      return toTemplateInstance(ordinal, template);
    }
    public Task<TemplateInstance> GetTemplateInstance(Tuple<Guid, int> templateRevisionId) =>
      GetTemplateInstance(templateRevisionId.Item1, templateRevisionId.Item2);
    public async Task<ExampleInstance> GetExampleInstance(Guid exampleId, int ordinal = default) {
      var example = await GetExample(exampleId);
      if (ordinal == default) ordinal = example.CurrentRevision.Ordinal;
      return await toExampleInstance(example, ordinal, GetTemplateInstance);
    }
    public Task<ExampleInstance> GetExampleInstance(Tuple<Guid,int> exampleRevisionId) {
      var (exampleId, ordinal) = exampleRevisionId;
      return GetExampleInstance(exampleId, ordinal);
    }
    public async Task<ExampleInstance> GetCurrentExampleInstance(Guid exampleId) {
      var example = await GetExample(exampleId);
      return await toCurrentExampleInstance(example, GetTemplateInstance);
    }

    private List<ClientEvent<TResult>> _deserializeClientEvents<TResult>(List<string> jsons) =>
      jsons.Select(deserializeFromJson<ClientEvent<TResult>>).ToList();

    private async Task<List<ClientEvent<TResult>>> _getStream<TResult>(string prefix, object id) {
      var jsons = await _jsRuntime.InvokeAsync<List<string>>(GET_STREAM, prefix, id);
      return _deserializeClientEvents<TResult>(jsons);
    }

    public Task<List<ClientEvent<User.Events.Event>>> GetUserStream(Guid id) => _getStream<User.Events.Event>(USER_PREFIX, id);
    public Task<List<ClientEvent<PrivateDeck.Events.Event>>> GetDeckStream(Guid id) => _getStream<PrivateDeck.Events.Event>(DECK_PREFIX, id);
    public Task<List<ClientEvent<PublicTemplate.Events.Event>>> GetTemplateStream(Guid id) => _getStream<PublicTemplate.Events.Event>(TEMPLATE_PREFIX, id);
    public Task<List<ClientEvent<Domain.Example.Events.Event>>> GetExampleStream(Guid id) => _getStream<Domain.Example.Events.Event>(EXAMPLE_PREFIX, id);
    public Task<List<ClientEvent<Stack.Events.Event>>> GetStackStream(Guid id) => _getStream<Stack.Events.Event>(STACK_PREFIX, id);

    public Task<List<ClientEvent<User.Events.Event>>> GetUserStream(List<Guid> id) => _getStream<User.Events.Event>(USER_PREFIX, id);
    public Task<List<ClientEvent<PrivateDeck.Events.Event>>> GetDeckStream(List<Guid> id) => _getStream<PrivateDeck.Events.Event>(DECK_PREFIX, id);
    public Task<List<ClientEvent<PublicTemplate.Events.Event>>> GetTemplateStream(List<Guid> id) => _getStream<PublicTemplate.Events.Event>(TEMPLATE_PREFIX, id);
    public Task<List<ClientEvent<Domain.Example.Events.Event>>> GetExampleStream(List<Guid> id) => _getStream<Domain.Example.Events.Event>(EXAMPLE_PREFIX, id);
    public Task<List<ClientEvent<Stack.Events.Event>>> GetStackStream(List<Guid> id) => _getStream<Stack.Events.Event>(STACK_PREFIX, id);

    public async Task<User.Fold.State> GetUserState(Guid id) => (await GetUserStream(id)).Select(ce => ce.Event).Pipe(User.Fold.foldInit.Invoke);
    public async Task<PrivateDeck.Fold.State> GetDeckState(Guid id) => (await GetDeckStream(id)).Select(ce => ce.Event).Pipe(PrivateDeck.Fold.foldInit.Invoke);
    public async Task<PublicTemplate.Fold.State> GetTemplateState(Guid id) => (await GetTemplateStream(id)).Select(ce => ce.Event).Pipe(PublicTemplate.Fold.foldInit.Invoke);
    public async Task<Domain.Example.Fold.State> GetExampleState(Guid id) => (await GetExampleStream(id)).Select(ce => ce.Event).Pipe(Domain.Example.Fold.foldInit.Invoke);
    public async Task<Stack.Fold.State> GetStackState(Guid id) => (await GetStackStream(id)).Select(ce => ce.Event).Pipe(Stack.Fold.foldInit.Invoke);

    public async Task<FSharpOption<Projection.Dexie.CardInstance>> GetNextQuizCard() {
      var stackJson = await _jsRuntime.InvokeAsync<string>(GET_NEXT_QUIZ_CARD);
      return Projection.Dexie.parseNextQuizCard(stackJson);
    }

    public async Task<List<ViewDeck>> GetViewDecks() {
      var elements = await _jsRuntime.InvokeAsync<List<JsonElement>>(GET_VIEW_DECKS);
      return elements.Select(e => {
        var dueCount = e.GetProperty("dueCount").GetInt32();
        var allCount = e.GetProperty("allCount").GetInt32();
        var summaryString = e.GetProperty("summary").GetString();
        var deck = deserializeFromJson<Summary.PrivateDeck>(summaryString);
        return new ViewDeck(deck.Id, deck.Visibility, deck.IsDefault, deck.Name, dueCount, allCount);
      }).ToList();
    }

    public async Task<FSharpOption<Summary.Stack>> GetStackByExample(Guid exampleId) {
      var json = await _jsRuntime.InvokeAsync<string>("getStackByExample", exampleId);
      return json == null
        ? FSharpOption<Summary.Stack>.None
        : deserializeFromJson<Summary.Stack>(json);
    }

  }
}
