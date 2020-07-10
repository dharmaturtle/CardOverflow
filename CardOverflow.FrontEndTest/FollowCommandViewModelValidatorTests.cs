using CardOverflow.Debug;
using CardOverflow.Server.Pages.Deck;
using CardOverflow.Test;
using FsCheck;
using FsCheck.Xunit;
using System.Linq;
using Xunit;
using static CardOverflow.FrontEndTest.TestHelper;

namespace CardOverflow.FrontEndTest {
  public class FollowCommandViewModelValidatorTests {

    [Fact]
    public void ValidNewDeck_IsValid() {
      var validNewDeckGen =
        from newDeckName in GeneratorsModule.stringOfLength(1, 250)
        from command in Arb.Generate<FollowCommandViewModel>()
        select SideEffect(command, x => {
          x.NewDeckName = newDeckName;
          x.FollowType = FollowType.NewDeck;
        });
      Prop.ForAll(Arb.From(validNewDeckGen), validNewDeckCommand => {
        var validator = new FollowCommandViewModelValidator();

        var result = validator.Validate(validNewDeckCommand);

        Assert.True(result.IsValid);
      }).QuickCheckThrowOnFailure();
    }

  }
}
