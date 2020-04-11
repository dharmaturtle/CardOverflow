using CardOverflow.Pure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.Server {
  public static class ImpureTools {

    public static ValueTask<object> Focus(this ElementReference elementRef, IJSRuntime jsRuntime) =>
      jsRuntime.InvokeAsync<object>("focusElement", elementRef);

  }
}
