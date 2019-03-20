using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class DeckCard
    {
        public int DeckId { get; set; }
        public int CardId { get; set; }

        public virtual Card Card { get; set; }
        public virtual Deck Deck { get; set; }
    }
}
