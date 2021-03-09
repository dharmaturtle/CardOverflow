using CardOverflow.Api;
using CardOverflow.Server.Data;
using Fluxor;
using LanguageExt;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.Server.Store {

  [Union]
  public interface UserState {
    UserState Loading();
    UserState Unauthenticated();
    UserState Authenticated(Domain.User.Events.Summary User);
  }

  public record GetUserAction { }
  public record GetUserOutcome {
    public Domain.User.Events.Summary User { get; init; }
  }

  public class UserStateFeature : AutoNameFeature<UserState> {
    protected override UserState GetInitialState() => UserStateCon.Loading();
  }

  public class UserStateEffects {
    private readonly KeyValueStore _kvs;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly UrlProvider _urlProvider;

    public UserStateEffects(
      KeyValueStore kvs,
      AuthenticationStateProvider authenticationStateProvider,
      UrlProvider urlProvider
    ) {
      _kvs = kvs;
      _authenticationStateProvider = authenticationStateProvider;
      _urlProvider = urlProvider;
    }

    [EffectMethod]
    public async Task _1(GetUserAction _, IDispatcher dispatcher) {
      var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
      if (state.User.Identity.IsAuthenticated) {
        var userId = state.User.Claims.Single(x =>
            x.Type == "sub" &&
            x.OriginalIssuer == _urlProvider.IdentityProvider.TrimEnd('/') &&
            x.Issuer == _urlProvider.IdentityProvider.TrimEnd('/')
          ).Value.Apply(Guid.Parse);
        var user = await _kvs.GetUser(userId).ToTask();
        dispatcher.Dispatch(new GetUserOutcome() { User = user });
      } else {
        dispatcher.Dispatch(new GetUserOutcome() { User = null }); // todo make record for this
      }
    }
  }

  public static class UserStateReducer {

    [ReducerMethod]
    public static UserState _1(UserState _, GetUserAction __) => UserStateCon.Loading();

    [ReducerMethod]
    public static UserState _2(UserState _, GetUserOutcome outcome) =>
      outcome.User == null
      ? UserStateCon.Unauthenticated()
      : UserStateCon.Authenticated(outcome.User);
  }

}
