using NUlid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardOverflow.Server {
  public static class Gulid {
    
    public static Guid Create() =>
      Ulid.NewUlid().ToGuid();

    public static List<Guid> Create(int count) =>
      Enumerable.Range(0, count).Select(_ => Create()).ToList();
  
  }
}
