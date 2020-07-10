using CardOverflow.Debug;
using CardOverflow.Pure;
using CardOverflow.Server.Pages.Deck;
using CardOverflow.Test;
using FsCheck;
using FsCheck.Xunit;
using System.Linq;
using Xunit;
using static CardOverflow.FrontEndTest.TestHelper;

namespace CardOverflow.FrontEndTest {
  public class FollowCommandViewModelValidatorTests {

    private static Gen<FollowCommandViewModel> _NewDeckGen(Gen<string> newDeckNameGen) =>
      from newDeckName in newDeckNameGen
      from command in Arb.Generate<FollowCommandViewModel>()
      select SideEffect(command, x => {
        x.NewDeckName = newDeckName;
        x.FollowType = FollowType.NewDeck;
      });

    [Fact]
    public void ValidNewDeck_IsValid() {
      var arb = GeneratorsModule.stringOfLength(1, 250).Pipe(_NewDeckGen).Pipe(Arb.From);
      Prop.ForAll(arb, validNewDeckCommand => {
        var validator = new FollowCommandViewModelValidator();

        var result = validator.Validate(validNewDeckCommand);

        Assert.True(result.IsValid);
      }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void InValidNewDeck_IsNotValid() {
      var arb =
        Gen.OneOf(
          GeneratorsModule.stringOfLength(0, 0),
          GeneratorsModule.stringOfLength(251, 500),
          Gen.Constant<string>(null)
        ).Pipe(_NewDeckGen).Pipe(Arb.From);
      Prop.ForAll(arb, validNewDeckCommand => {
        var validator = new FollowCommandViewModelValidator();

        var result = validator.Validate(validNewDeckCommand);

        Assert.False(result.IsValid);
      }).QuickCheckThrowOnFailure();
    }

  }
}
