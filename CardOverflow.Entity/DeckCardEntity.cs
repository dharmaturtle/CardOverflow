using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class DeckCardEntity
    {
        public int DeckId { get; set; }
        public int CardId { get; set; }

        public virtual CardEntity Card { get; set; }
        public virtual DeckEntity Deck { get; set; }
    }
}
