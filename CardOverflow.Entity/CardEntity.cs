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
        public MemorizationStateAndCardStateEnum MemorizationStateAndCardState { get; set; }
        public byte LapseCount { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalNegativeIsMinutesPositiveIsDays { get; set; }
        public byte? StepsIndex { get; set; }
        public int CardOptionId { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Due { get; set; }

        [ForeignKey("CardOptionId")]
        [InverseProperty("Cards")]
        public virtual CardOptionEntity CardOption { get; set; }
        [ForeignKey("ConceptId")]
        [InverseProperty("Cards")]
        public virtual ConceptEntity Concept { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<DeckCardEntity> DeckCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
    }
}
