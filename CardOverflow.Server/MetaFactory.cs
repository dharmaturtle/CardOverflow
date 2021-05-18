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

    public MetaFactory(IClock clock) => _clock = clock;

    public Meta Create(Guid userId) =>
      new(null, _clock.GetCurrentInstant(), Guid.NewGuid(), userId);

  }
}
