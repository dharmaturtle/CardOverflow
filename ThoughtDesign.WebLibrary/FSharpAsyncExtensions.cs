using Microsoft.FSharp.Core;
using System.Threading.Tasks;
using Microsoft.FSharp.Control;
using System.Threading;

namespace ThoughtDesign.WebLibrary {

  public static class FSharpAsyncExtensions {
    public static Task<T> ToTask<T>(this FSharpAsync<T> item) =>
      FSharpAsync.StartAsTask(
        item,
        FSharpOption<TaskCreationOptions>.None,
        FSharpOption<CancellationToken>.None
      );
  }

}
