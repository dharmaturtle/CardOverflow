using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class Card
    {
        public Card()
        {
            CardDecks = new HashSet<CardDeck>();
        }

        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }

        public virtual ICollection<CardDeck> CardDecks { get; set; }
    }
}
