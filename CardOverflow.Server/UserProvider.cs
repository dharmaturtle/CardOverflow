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
using Microsoft.AspNetCore.Components.Authorization;
using ThoughtDesign.WebLibrary;
using Blazored.Toast.Services;

namespace CardOverflow.Server {
  public class UserProvider {
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly UrlProvider _urlProvider;
    private readonly Dexie _dexie;
    private readonly IToastService _toastService;

    public UserProvider(
      AuthenticationStateProvider authenticationStateProvider,
      UrlProvider urlProvider,
      Dexie dexie,
      IToastService toastService
    ) {
      _authenticationStateProvider = authenticationStateProvider;
      _urlProvider = urlProvider;
      _dexie = dexie;
      _toastService = toastService;
    }

    public async Task<FSharpOption<Summary.User>> GetSummary() {
      var userId = await GetId();
      return userId == null
        ? FSharpOption<Summary.User>.None
        : await _dexie.GetUser(userId.Value);
    }

    public async Task<Summary.User> ForceSummary() {
      var summary = await GetSummary();
      if (summary.IsSome()) {
        return summary.Value;
      }
      _toastService.ShowError("You aren't logged in.");
      throw new Exception("You aren't logged in");
    }

    public async Task<bool> IsAuthenticated() {
      var maybeId = await GetId();
      return maybeId.HasValue;
    }

    public async Task<Guid?> GetId() {
      var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
      if (authState.User.Identity.IsAuthenticated) {
        var userId = authState.User.Claims.Single(x =>
            x.Type == "sub" &&
            x.OriginalIssuer == _urlProvider.IdentityProvider &&
            x.Issuer == _urlProvider.IdentityProvider
          ).Value;
        return Guid.Parse(userId);
      }
      return null;
    }

  }
}
