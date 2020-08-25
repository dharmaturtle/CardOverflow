using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;

namespace CardOverflow.Entity
{
    public partial class CardSettingEntity
    {
        public CardSettingEntity()
        {
            Cards = new HashSet<CardEntity>();
            User_Grompleafs = new HashSet<User_GrompleafEntity>();
        }

        [Key]
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
        [Required]
        [StringLength(100)]
        public string NewCardsStepsInMinutes {
            get => _NewCardsStepsInMinutes;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and NewCardsStepsInMinutes has a maximum length of 100. Attempted value: {value}");
                _NewCardsStepsInMinutes = value;
            }
        }
        private string _NewCardsStepsInMinutes;
        public short NewCardsMaxPerDay { get; set; }
        public short NewCardsGraduatingIntervalInDays { get; set; }
        public short NewCardsEasyIntervalInDays { get; set; }
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
        public string LapsedCardsStepsInMinutes {
            get => _LapsedCardsStepsInMinutes;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LapsedCardsStepsInMinutes has a maximum length of 100. Attempted value: {value}");
                _LapsedCardsStepsInMinutes = value;
            }
        }
        private string _LapsedCardsStepsInMinutes;
        public short LapsedCardsNewIntervalFactorInPermille { get; set; }
        public short LapsedCardsMinimumIntervalInDays { get; set; }
        public short LapsedCardsLeechThreshold { get; set; }
        public bool ShowAnswerTimer { get; set; }
        public bool AutomaticallyPlayAudio { get; set; }
        public bool ReplayQuestionAudioOnAnswer { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("CardSettings")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CardSetting")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("DefaultCardSetting")]
        public virtual ICollection<User_GrompleafEntity> User_Grompleafs { get; set; }
    }
}

