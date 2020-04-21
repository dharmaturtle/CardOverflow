using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class UserEntity
    {
        public UserEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            CardSettings = new HashSet<CardSettingEntity>();
            Cards = new HashSet<CardEntity>();
            CommentCards = new HashSet<CommentCardEntity>();
            CommentTemplates = new HashSet<CommentTemplateEntity>();
            CommunalFields = new HashSet<CommunalFieldEntity>();
            Feedbacks = new HashSet<FeedbackEntity>();
            Filters = new HashSet<FilterEntity>();
            Templates = new HashSet<TemplateEntity>();
            User_TemplateInstances = new HashSet<User_TemplateInstanceEntity>();
            Vote_CommentCards = new HashSet<Vote_CommentCardEntity>();
            Vote_CommentTemplates = new HashSet<Vote_CommentTemplateEntity>();
            Vote_Feedbacks = new HashSet<Vote_FeedbackEntity>();
        }

        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(32)]
        public string DisplayName {
            get => _DisplayName;
            set {
                if (value.Length > 32) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and DisplayName has a maximum length of 32. Attempted value: {value}");
                _DisplayName = value;
            }
        }
        private string _DisplayName;
        public int? DefaultCardSettingId { get; set; }
        public bool ShowNextReviewTime { get; set; }
        public bool ShowRemainingCardCount { get; set; }
        public short MixNewAndReview { get; set; }
        public short NextDayStartsAtXHoursPastMidnight { get; set; }
        public short LearnAheadLimitInMinutes { get; set; }
        public short TimeboxTimeLimitInMinutes { get; set; }
        public bool IsNightMode { get; set; }

        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CardSettingEntity> CardSettings { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentTemplateEntity> CommentTemplates { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<CommunalFieldEntity> CommunalFields { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<FeedbackEntity> Feedbacks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<FilterEntity> Filters { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<TemplateEntity> Templates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<User_TemplateInstanceEntity> User_TemplateInstances { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentCardEntity> Vote_CommentCards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentTemplateEntity> Vote_CommentTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_FeedbackEntity> Vote_Feedbacks { get; set; }
    }
}

