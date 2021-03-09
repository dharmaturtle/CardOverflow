using Fluxor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.Server.Store {

  public record CounterState {
    public int Count { get; init; }
  }

  public record IncreaseCounter {
    public int Step { get; set; } = 1;
  }

  public class CounterFeature : AutoNameFeature<CounterState> {
    protected override CounterState GetInitialState() => new() {
      Count = 0,
    };
  }

  public static class CounterReducer {
    
    [ReducerMethod]
    public static CounterState OnIncreaseCounter(CounterState state, IncreaseCounter action) => state with {
      Count = state.Count + action.Step
    };

  }

}
