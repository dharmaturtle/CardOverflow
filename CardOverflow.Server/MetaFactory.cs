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
  public class MetaFactory {
    private readonly IClock _clock;
    private readonly UserProvider _userProvider;

    public MetaFactory(IClock clock, UserProvider userProvider) {
      _clock = clock;
      _userProvider = userProvider;
    }

    public async Task<Meta> Create() {
      var userId = await _userProvider.ForceId();
      return new (null, _clock.GetCurrentInstant(), null, Guid.NewGuid(), userId);
    }

  }
}
