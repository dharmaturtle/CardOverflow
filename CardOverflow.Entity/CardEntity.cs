using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class CardEntity
    {
        public CardEntity()
        {
            DeckCards = new HashSet<DeckCardEntity>();
            Histories = new HashSet<HistoryEntity>();
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

        public virtual ConceptEntity Concept { get; set; }
        public virtual ICollection<DeckCardEntity> DeckCards { get; set; }
        public virtual ICollection<HistoryEntity> Histories { get; set; }
    }
}
