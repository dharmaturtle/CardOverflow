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
using FsCheck;
using FsCheck.Xunit;

namespace CardOverflow.FrontEndTest {
  public class FollowButtonTests : TestContext {

    //private static class FollowButtonTestsArb {
    //  public static Arbitrary<Domain.User.Events.Summary> UserSummary() =>
    //    (from name in Arb.Generate<string>()
    //     from email in Arb.Generate<string>()
    //     from id in Arb.Generate<Guid>()
    //     select new Domain.User.Events.Summary(id, name, email)
    //    ).Pipe(Arb.From);
    //  public static Arbitrary<DeckEntity> DeckEntity() =>
    //    Arb.Generate<Guid>()
    //    .Select(id => new DeckEntity { Id = id })
    //    .Pipe(Arb.From);
    //}

    //[Property(DisplayName = "Submitting default FollowDeckCommand _ displays validation error", Arbitrary = new[] { typeof(FollowButtonTestsArb) })]
    public bool _1(Domain.Summary.User userSummary, DeckWithFollowMeta deckWithFollowMeta, DeckEntity usersDefaultDeck) {
      Setup(Services, db => new UserEntity {
        Id = userSummary.Id,
        DefaultDeckId = usersDefaultDeck.Id,
        Decks = new List<DeckEntity> { usersDefaultDeck }
      }.Pipe(db.User.Add));
      var counter = RenderComponent<FollowButtons>(
        (nameof(FollowButtons.Deck), deckWithFollowMeta),
        CascadingValue(Task.FromResult(userSummary))
      );

      counter.Find("form").Submit();

      return "'New Deck Name' must not be empty." == counter.Find(".validation-message").TextContent;
    }

  }
}
