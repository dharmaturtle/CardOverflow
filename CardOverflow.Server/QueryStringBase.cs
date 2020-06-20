using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.Server {
  public abstract class QueryStringBase : ComponentBase, IDisposable {
    [Inject] NavigationManager NavigationManager { get; set; }

    protected override void OnInitialized() {
      GetQueryStringValues();
      NavigationManager.LocationChanged += HandleLocationChanged; // https://chrissainty.com/working-with-query-strings-in-blazor/
    }

    private void HandleLocationChanged(object sender, LocationChangedEventArgs e) {
      GetQueryStringValues();
      StateHasChanged();
    }

    protected abstract void GetQueryStringValues();

    public void Dispose() =>
      NavigationManager.LocationChanged -= HandleLocationChanged;

  }
}
