using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CardOverflow.Web.Helpers {
  public static class Extensions {

    public static Task Focus(this ElementRef elementRef, IJSRuntime jsRuntime) =>
      jsRuntime.InvokeAsync<object>("focusElement", elementRef);

  }
}
