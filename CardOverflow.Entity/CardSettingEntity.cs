using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class CardSettingEntity
    {
        public CardSettingEntity()
        {
            Cards = new HashSet<CardEntity>();
            User_TemplateRevisions = new HashSet<User_TemplateRevisionEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 100. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public Period[] NewCardsSteps { get; set; }
        public int NewCardsMaxPerDay { get; set; }
        public Period NewCardsGraduatingInterval { get; set; }
        public Period NewCardsEasyInterval { get; set; }
        public int NewCardsStartingEaseFactorInPermille { get; set; }
        public bool NewCardsBuryRelated { get; set; }
        public int MatureCardsMaxPerDay { get; set; }
        public int MatureCardsEaseFactorEasyBonusFactorInPermille { get; set; }
        public int MatureCardsIntervalFactorInPermille { get; set; }
        public Period MatureCardsMaximumInterval { get; set; }
        public int MatureCardsHardIntervalFactorInPermille { get; set; }
        public bool MatureCardsBuryRelated { get; set; }
        public Period[] LapsedCardsSteps { get; set; }
        public int LapsedCardsNewIntervalFactorInPermille { get; set; }
        public Period LapsedCardsMinimumInterval { get; set; }
        public int LapsedCardsLeechThreshold { get; set; }
        public bool ShowAnswerTimer { get; set; }
        public bool AutomaticallyPlayAudio { get; set; }
        public bool ReplayQuestionAudioOnAnswer { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("CardSettings")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CardSetting")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("DefaultCardSetting")]
        public virtual ICollection<User_TemplateRevisionEntity> User_TemplateRevisions { get; set; }
    }
}

