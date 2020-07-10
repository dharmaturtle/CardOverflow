using System;

namespace CardOverflow.FrontEndTest {
  public static class TestHelper {
    
    public static T SideEffect<T>(T input, Action<T> action) {
      action(input);
      return input;
    }

    public static bool Not(bool input) => !input;

  }
}
