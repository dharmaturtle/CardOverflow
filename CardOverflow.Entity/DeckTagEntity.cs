using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class DeckTagEntity
    {
        public int DeckId { get; set; }
        public int TagId { get; set; }

        public virtual DeckEntity Deck { get; set; }
        public virtual TagEntity Tag { get; set; }
    }
}
