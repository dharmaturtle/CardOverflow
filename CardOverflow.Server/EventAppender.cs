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
using Blazored.Toast.Services;
using CardOverflow.Pure;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.Server {
  using static EventAppender.Deck;
  public class DeckAppender {
    private readonly Dexie _dexie;
    private readonly MetaFactory _metaFactory;
    private readonly IToastService _toastService;
    private readonly Appender _appender;
    
    public DeckAppender(Dexie dexie, MetaFactory metaFactory, IToastService toastService, Appender appender) {
      _dexie = dexie;
      _metaFactory = metaFactory;
      _toastService = toastService;
      _appender = appender;
    }

    public async Task<bool> Create(Guid userId, string name) {
      var meta = await _metaFactory.Create();
      var deckId = Guid.NewGuid();
      var created = new Deck.Events.Created(meta, deckId, Visibility.Private, name, "");
      var state = await _dexie.GetDeckState(deckId);
      return await _transact(deckId, Deck.decideCreate(created, state));
    }

    public async Task<bool> Discard(Guid userId, Guid deckId) {
      var meta = await _metaFactory.Create();
      var discarded = new Deck.Events.Discarded(meta);
      var state = await _dexie.GetDeckState(deckId);
      return await _transact(deckId, Deck.decideDiscarded(discarded, state));
    }

    public async Task<bool> _transact(Guid streamId, Tuple<FSharpResult<Unit, string>, FSharpList<Deck.Events.Event>> x) {
      var (r, events) = x;
      if (r.IsOk) {
        await events
          .Select(e => new ClientEvent<Deck.Events.Event>(streamId, e))
          .Pipe(_dexie.Append);
        return true;
      } else {
        _toastService.ShowError(r.ErrorValue);
        return false;
      }
    }

    public async Task Sync() {
      var unsynced = await _dexie.GetDeckUnsynced();
      var r = await _appender.Sync(unsynced).ToTask();
      if (r.IsOk) {
        _toastService.ShowSuccess("Sync successful!");
      } else {
        _toastService.ShowError(r.ErrorValue);
      }
    }

  }
}

namespace CardOverflow.Server {
  using static EventAppender.User;
  public class UserAppender {
    private readonly Dexie _dexie;
    private readonly MetaFactory _metaFactory;
    private readonly IToastService _toastService;
    private readonly Appender _appender;

    public UserAppender(Dexie dexie, MetaFactory metaFactory, IToastService toastService, Appender appender) {
      _dexie = dexie;
      _metaFactory = metaFactory;
      _toastService = toastService;
      _appender = appender;
    }

    public async Task<bool> CardSettingsEdited(Guid userId, List<CardSetting> cardSettings) {
      var meta = await _metaFactory.Create();
      var edited = new User.Events.CardSettingsEdited(meta, cardSettings.ToFList());
      var state = await _dexie.GetUserState(userId);
      return await _transact(userId, User.decideCardSettingsEdited(edited, state));
    }

    public async Task<bool> _transact(Guid streamId, Tuple<FSharpResult<Unit, string>, FSharpList<User.Events.Event>> x) {
      var (r, events) = x;
      if (r.IsOk) {
        await events
          .Select(e => new ClientEvent<User.Events.Event>(streamId, e))
          .Pipe(_dexie.Append);
        return true;
      } else {
        _toastService.ShowError(r.ErrorValue);
        return false;
      }
    }

    public async Task Sync() {
      var unsynced = await _dexie.GetUserUnsynced();
      var r = await _appender.Sync(unsynced).ToTask();
      if (r.IsOk) {
        _toastService.ShowSuccess("Sync successful!");
      } else {
        _toastService.ShowError(r.ErrorValue);
      }
    }

  }
}

namespace CardOverflow.Server {
  using static EventAppender.Template;
  public class TemplateAppender {
    private readonly Dexie _dexie;
    private readonly MetaFactory _metaFactory;
    private readonly IToastService _toastService;
    private readonly Appender _appender;

    public TemplateAppender(Dexie dexie, MetaFactory metaFactory, IToastService toastService, Appender appender) {
      _dexie = dexie;
      _metaFactory = metaFactory;
      _toastService = toastService;
      _appender = appender;
    }

    public async Task<bool> Edit(Guid userId, Summary.TemplateRevision revision, Guid templateId) {
      var meta = await _metaFactory.Create();
      var edited = Template.Events.Edited.fromRevision(revision, meta);
      var state = await _dexie.GetTemplateState(templateId);
      return await _transact(templateId, Template.decideEdit(edited, templateId, state));
    }

    public async Task<bool> _transact(Guid streamId, Tuple<FSharpResult<Unit, string>, FSharpList<Template.Events.Event>> x) {
      var (r, events) = x;
      if (r.IsOk) {
        await events
          .Select(e => new ClientEvent<Template.Events.Event>(streamId, e))
          .Pipe(_dexie.Append);
        return true;
      } else {
        _toastService.ShowError(r.ErrorValue);
        return false;
      }
    }

    public async Task Sync() {
      var unsynced = await _dexie.GetTemplateUnsynced();
      var r = await _appender.Sync(unsynced).ToTask();
      if (r.IsOk) {
        _toastService.ShowSuccess("Sync successful!");
      } else {
        _toastService.ShowError(r.ErrorValue);
      }
    }

  }
}

namespace CardOverflow.Server {
  using static EventAppender.Example;
  using Domain;
  public class ExampleAppender {
    private readonly Dexie _dexie;
    private readonly MetaFactory _metaFactory;
    private readonly IToastService _toastService;
    private readonly Appender _appender;

    public ExampleAppender(Dexie dexie, MetaFactory metaFactory, IToastService toastService, Appender appender) {
      _dexie = dexie;
      _metaFactory = metaFactory;
      _toastService = toastService;
      _appender = appender;
    }

    public async Task<bool> Handle(Example.Events.Event e, Guid exampleId) {
      if (e.IsCreated) {
        var created = ((Example.Events.Event.Created) e).Item;
        var template = await _dexie.GetTemplateState(created.TemplateRevisionId.Item1);
        var example = await _dexie.GetExampleState(created.Id);
        return await _transact(created.Id, Example.decideCreate(template, created, example));
      } else if (e.IsEdited) {
        var edited = ((Example.Events.Event.Edited) e).Item;
        var template = await _dexie.GetTemplateState(edited.TemplateRevisionId.Item1);
        var example = await _dexie.GetExampleState(exampleId);
        return await _transact(exampleId, Example.decideEdit(template, edited, exampleId, example));
      } else throw new Exception("Unsupported event:" + e.GetType().FullName);
    }

    public async Task<bool> _transact(Guid streamId, Tuple<FSharpResult<Unit, string>, FSharpList<Example.Events.Event>> x) {
      var (r, events) = x;
      if (r.IsOk) {
        await events
          .Select(e => new ClientEvent<Example.Events.Event>(streamId, e))
          .Pipe(_dexie.Append);
        return true;
      } else {
        _toastService.ShowError(r.ErrorValue);
        return false;
      }
    }

    public async Task Sync() {
      var unsynced = await _dexie.GetExampleUnsynced();
      var r = await _appender.Sync(unsynced).ToTask();
      if (r.IsOk) {
        _toastService.ShowSuccess("Sync successful!");
      } else {
        _toastService.ShowError(r.ErrorValue);
      }
    }

  }
}
