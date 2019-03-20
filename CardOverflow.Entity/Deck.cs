﻿using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class Deck
    {
        public Deck()
        {
            DeckCards = new HashSet<DeckCard>();
            DeckTags = new HashSet<DeckTag>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<DeckCard> DeckCards { get; set; }
        public virtual ICollection<DeckTag> DeckTags { get; set; }
    }
}
