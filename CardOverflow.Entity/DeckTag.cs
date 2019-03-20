using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class DeckTag
    {
        public int DeckId { get; set; }
        public int TagId { get; set; }

        public virtual Deck Deck { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
