using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class Deck
    {
        public Deck()
        {
            CardDecks = new HashSet<CardDeck>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<CardDeck> CardDecks { get; set; }
    }
}
