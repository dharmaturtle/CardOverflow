using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class Card
    {
        public Card()
        {
            DeckCards = new HashSet<DeckCard>();
            Histories = new HashSet<History>();
        }

        public int Id { get; set; }
        public int ConceptId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime Modified { get; set; }
        public byte MemorizationStateAndCardState { get; set; }
        public byte LapseCount { get; set; }
        public short EaseFactor { get; set; }
        public short Interval { get; set; }
        public byte? ReviewsUntilGraduation { get; set; }

        public virtual Concept Concept { get; set; }
        public virtual ICollection<DeckCard> DeckCards { get; set; }
        public virtual ICollection<History> Histories { get; set; }
    }
}
