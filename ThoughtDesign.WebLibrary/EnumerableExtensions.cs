using CardOverflow.Pure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThoughtDesign.WebLibrary {

  public static class ListX {
    public static List<T> Singleton<T>(T item) =>
      new List<T>() { item };
  }
  
  public static class FList {
    public static FSharpList<T> Singleton<T>(T item) =>
      new List<T>() { item }.ToFList();
  }

}
