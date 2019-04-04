using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Card")]
    public partial class CardEntity
    {
        public CardEntity()
        {
            DeckCards = new HashSet<DeckCardEntity>();
            Histories = new HashSet<HistoryEntity>();
        }

        public int Id { get; set; }
        public int ConceptId { get; set; }
        [Required]
        [StringLength(1028)]
        public string Question { get; set; }
        [Required]
        [StringLength(1028)]
        public string Answer { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Modified { get; set; }
        public byte MemorizationStateAndCardState { get; set; }
        public byte LapseCount { get; set; }
        public short EaseFactor { get; set; }
        public short Interval { get; set; }
        public byte? ReviewsUntilGraduation { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("Cards")]
        public virtual ConceptEntity Concept { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<DeckCardEntity> DeckCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
    }
}
