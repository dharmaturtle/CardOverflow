using Blazored.Toast.Services;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FSharp.Core;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.Server {
  public static class ImpureTools {

    public static ValueTask<object> Focus(this ElementReference elementRef, IJSRuntime jsRuntime) =>
      jsRuntime.InvokeAsync<object>("focusElement", elementRef);

    public static async Task Match<TOk>(this Task<FSharpResult<TOk, string>> tr, IToastService toastService, Action<TOk> onOk) {
      var r = await tr;
      if (r.IsOk) {
        onOk(r.ResultValue);
      } else {
        toastService.ShowError(r.ErrorValue);
      }
    }

  }
}
