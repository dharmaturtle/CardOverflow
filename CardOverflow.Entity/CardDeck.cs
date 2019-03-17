using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class CardDeck
    {
        public int CardId { get; set; }
        public int DeckId { get; set; }

        public virtual Card Card { get; set; }
        public virtual Deck Deck { get; set; }
    }
}
