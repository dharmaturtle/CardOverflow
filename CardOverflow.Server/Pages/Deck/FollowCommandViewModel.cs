using CardOverflow.Pure;
using CardOverflow.Sanitation;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CardOverflow.Server.Pages.Deck {
  
  public enum FollowType {
    NewDeck,
    OldDeck,
    NoDeck,
  }

  public class FollowCommandViewModel {
    public FollowType FollowType { get; set; } = FollowType.NewDeck;
    public bool NotifyOfAnyNewChanges { get; set; } = true;
    [StringLength(250, MinimumLength = 1, ErrorMessage = "Deck names must be 1 - 250 characters long.")] // medTODO 250 needs to be tied to the DB max somehow
    public string NewDeckName { get; set; }
    public FSharpOption<bool> EditExisting { get; set; } = FSharpOption<bool>.None;
    public int OldDeckId { get; set; }
    public SanitizeDeckRepository.FollowDeckType FollowTypeDU() => FollowType switch
    {
      FollowType.NewDeck => SanitizeDeckRepository.FollowDeckType.NewNewDeck(NewDeckName),
      FollowType.OldDeck => SanitizeDeckRepository.FollowDeckType.NewOldDeck(OldDeckId),
      FollowType.NoDeck => SanitizeDeckRepository.FollowDeckType.NoDeck,
      var x => throw new Exception($"Unsupported FollowType: {x}")
    };
  }

}
