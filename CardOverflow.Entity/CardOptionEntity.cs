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
        public string NewCardsStepsInMinutes { get; set; }
        public short NewCardsMaxPerDay { get; set; }
        public byte NewCardsGraduatingIntervalInDays { get; set; }
        public byte NewCardsEasyIntervalInDays { get; set; }
        public short NewCardsStartingEaseFactorInPermille { get; set; }
        public bool NewCardsBuryRelated { get; set; }
        public short MatureCardsMaxPerDay { get; set; }
        public short MatureCardsEaseFactorEasyBonusFactorInPermille { get; set; }
        public short MatureCardsIntervalFactorInPermille { get; set; }
        public short MatureCardsMaximumIntervalInDays { get; set; }
        public short MatureCardsHardIntervalFactorInPermille { get; set; }
        public bool MatureCardsBuryRelated { get; set; }
        [Required]
        [StringLength(100)]
        public string LapsedCardsStepsInMinutes { get; set; }
        public short LapsedCardsNewIntervalInPermille { get; set; }
        public byte LapsedCardsMinimumIntervalInDays { get; set; }
        public byte LapsedCardsLeechThreshold { get; set; }
        public bool ShowAnswerTimer { get; set; }
        public bool AutomaticallyPlayAudio { get; set; }
        public bool ReplayQuestionAudioOnAnswer { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("CardOptions")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CardOption")]
        public virtual ICollection<CardEntity> Cards { get; set; }
    }
}
