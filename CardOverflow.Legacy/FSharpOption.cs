using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CardOverflow.Legacy {
  // https://docs.microsoft.com/en-us/archive/blogs/jaredpar/making-f-type-inference-friendly-for-c
  public static class FSharpOption {
    
    public static FSharpOption<T> Create<T>(T value) =>
      new FSharpOption<T>(value);

    public static bool IsSome<T>(this FSharpOption<T> opt) =>
      FSharpOption<T>.get_IsSome(opt);

    public static bool IsNone<T>(this FSharpOption<T> opt) =>
      FSharpOption<T>.get_IsNone(opt);

  }
}
