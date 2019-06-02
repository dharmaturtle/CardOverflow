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
            PrivateTagCards = new HashSet<PrivateTagCardEntity>();
            PublicTagCards = new HashSet<PublicTagCardEntity>();
        }

        public int Id { get; set; }
        public int ConceptId { get; set; }
        public MemorizationStateAndCardStateEnum MemorizationStateAndCardState { get; set; }
        public byte LapseCount { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalNegativeIsMinutesPositiveIsDays { get; set; }
        public byte? StepsIndex { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Due { get; set; }
        public byte TemplateIndex { get; set; }
        public int CardOptionId { get; set; }

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
        [InverseProperty("Card")]
        public virtual ICollection<PrivateTagCardEntity> PrivateTagCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<PublicTagCardEntity> PublicTagCards { get; set; }
    }
}
