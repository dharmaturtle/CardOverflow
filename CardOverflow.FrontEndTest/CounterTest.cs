using CardOverflow.Debug;
using CardOverflow.Pure;
using CardOverflow.Server.Pages;
using CardOverflow.Server.Pages.Deck;
using CardOverflow.Test;
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Linq;
using Xunit;
using Bunit;
using static CardOverflow.FrontEndTest.TestHelper;

namespace CardOverflow.FrontEndTest {
  public class CounterTest : TestContext {

    [Fact]
    public void ButtonClicked_Increments() {
      var counter = RenderComponent<Counter>();

      counter.Find("button").Click();

      counter.Find("p").MarkupMatches("<p>Current count: 1</p>");
    }

  }
}
