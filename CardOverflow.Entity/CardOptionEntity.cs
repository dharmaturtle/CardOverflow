using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("CardOption")]
    public partial class CardOptionEntity
    {
        public CardOptionEntity()
        {
            Cards = new HashSet<CardEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string NewCardsSteps { get; set; }
        public short NewCardsMaxPerDay { get; set; }
        public byte NewCardsGraduatingInterval { get; set; }
        public byte NewCardsEasyInterval { get; set; }
        public short NewCardsStartingEase { get; set; }
        public bool NewCardsBuryRelated { get; set; }
        public short MatureCardsMaxPerDay { get; set; }
        public short MatureCardsEasyBonus { get; set; }
        public short MatureCardsIntervalModifier { get; set; }
        public short MatureCardsMaximumInterval { get; set; }
        public bool MatureCardsBuryRelated { get; set; }
        [Required]
        [StringLength(100)]
        public string LapsedCardsSteps { get; set; }
        public short LapsedCardsNewInterval { get; set; }
        public byte LapsedCardsMinimumInterval { get; set; }
        public byte LapsedCardsLeechThreshold { get; set; }
        public bool ShowAnswerTimer { get; set; }
        public bool AutomaticallyPlayAudio { get; set; }
        public bool ReplayQuestionAnswerAudioOnAnswer { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("CardOptions")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CardOption")]
        public virtual ICollection<CardEntity> Cards { get; set; }
    }
}
