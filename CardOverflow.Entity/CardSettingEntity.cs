using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardSettingEntity
    {
        public CardSettingEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            User_TemplateInstances = new HashSet<User_TemplateInstanceEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
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
        public string LapsedCardsStepsInMinutes {
            get => _LapsedCardsStepsInMinutes;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LapsedCardsStepsInMinutes has a maximum length of 100. Attempted value: {value}");
                _LapsedCardsStepsInMinutes = value;
            }
        }
        private string _LapsedCardsStepsInMinutes;
        public short LapsedCardsNewIntervalFactorInPermille { get; set; }
        public byte LapsedCardsMinimumIntervalInDays { get; set; }
        public byte LapsedCardsLeechThreshold { get; set; }
        public bool ShowAnswerTimer { get; set; }
        public bool AutomaticallyPlayAudio { get; set; }
        public bool ReplayQuestionAudioOnAnswer { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("CardSettings")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CardSetting")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("DefaultCardSetting")]
        public virtual ICollection<User_TemplateInstanceEntity> User_TemplateInstances { get; set; }
    }
}

