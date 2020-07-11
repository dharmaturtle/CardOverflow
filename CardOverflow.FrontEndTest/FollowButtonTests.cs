using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using CardOverflow.Server;
using ThoughtDesign.WebLibrary;
using CardOverflow.Api;
using BlazorStrap;
using FluentValidation;
using CardOverflow.Sanitation;
using System.Threading.Tasks;
using CardOverflow.Entity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Bunit;
using Xunit;
using static CardOverflow.FrontEndTest.TestHelper;
using static Bunit.ComponentParameterFactory;
using System;
using CardOverflow.Server.Pages.Deck;
using CardOverflow.Pure;

namespace CardOverflow.FrontEndTest {
  public class FollowButtonTests : TestContext {
    [Fact(DisplayName = "Submitting default FollowDeckCommand _ displays validation error")]
    public async Task _1() {
      const int userId = 1;
      const int defaultDeckId = 123;
      await Setup(Services, db => new UserEntity {
        Id = userId,
        DefaultDeckId = defaultDeckId,
        Decks = new List<DeckEntity> {
          new DeckEntity {
            Id = defaultDeckId,
            Name = Guid.NewGuid().ToString(),
          }
        }
      }.Pipe(db.User.Add));
      var claims = new UserClaims(userId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()).Pipe(Task.FromResult);
      var counter = RenderComponent<FollowButtons>(
        (nameof(FollowButtons.Deck), new PublicDeck()),
        CascadingValue(claims)
      );

      counter.Find("form").Submit();

      Assert.Equal("'New Deck Name' must not be empty.", counter.Find(".validation-message").TextContent);
    }

  }
}
