using Microsoft.FSharp.Core;
using System.Threading.Tasks;
using Microsoft.FSharp.Control;
using System.Threading;
using CardOverflow.Legacy;
using System;

namespace ThoughtDesign.WebLibrary {

  public static class FSharpExtensions {
    public static Task<T> ToTask<T>(this FSharpAsync<T> item) =>
      FSharpAsync.StartAsTask(
        item,
        FSharpOption<TaskCreationOptions>.None,
        FSharpOption<CancellationToken>.None
      );

    public static bool ExampleRevisionId(this FSharpOption<Domain.Summary.Stack> stack, out Tuple<Guid, int> exampleRevisionId) {
      var b = stack.IsSome() && stack.Value.ExampleRevisionId.IsSome();
      exampleRevisionId = b ? stack.Value.ExampleRevisionId.Value : null;
      return b;
    }
  }

}
