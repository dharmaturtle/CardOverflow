using CardOverflow.Pure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("padawan")]
    public partial class UserEntity
    {
        public UserEntity()
        {
            Cards = new HashSet<CardEntity>();
            Examples = new HashSet<ExampleEntity>();
            CardSettings = new HashSet<CardSettingEntity>();
            Templates = new HashSet<TemplateEntity>();
            CommentTemplates = new HashSet<CommentTemplateEntity>();
            CommentConcepts = new HashSet<CommentConceptEntity>();
            DeckFollowers = new HashSet<DeckFollowerEntity>();
            Decks = new HashSet<DeckEntity>();
            Feedbacks = new HashSet<FeedbackEntity>();
            Filters = new HashSet<FilterEntity>();
            Histories = new HashSet<HistoryEntity>();
            SentNotifications = new HashSet<NotificationEntity>();
            ReceivedNotifications = new HashSet<ReceivedNotificationEntity>();
            Concepts = new HashSet<ConceptEntity>();
            User_TemplateRevisions = new HashSet<User_TemplateRevisionEntity>();
            Vote_CommentTemplates = new HashSet<Vote_CommentTemplateEntity>();
            Vote_CommentConcepts = new HashSet<Vote_CommentConceptEntity>();
            Vote_Feedbacks = new HashSet<Vote_FeedbackEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
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
        public Guid DefaultCardSettingId { get; set; }
        public Guid DefaultDeckId { get; set; }
        public bool ShowNextReviewTime { get; set; }
        public bool ShowRemainingCardCount { get; set; }
        public StudyOrder StudyOrder { get; set; }
        public LocalTime NextDayStartsAt { get; set; }
        public LocalTime LearnAheadLimit { get; set; }
        public LocalTime TimeboxTimeLimit { get; set; }
        public bool IsNightMode { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        public TimezoneName Timezone { get; set; }
        [Required]
        [StringLength(300)]
        public string[] CardTags { get; set; } = new string[0];

        [ForeignKey("DefaultCardSettingId")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("DefaultDeckId")]
        public virtual DeckEntity DefaultDeck { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<ExampleEntity> Examples { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CardSettingEntity> CardSettings { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<TemplateEntity> Templates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentTemplateEntity> CommentTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentConceptEntity> CommentConcepts { get; set; }
        [InverseProperty("Follower")]
        public virtual ICollection<DeckFollowerEntity> DeckFollowers { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<FeedbackEntity> Feedbacks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<FilterEntity> Filters { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("Sender")]
        public virtual ICollection<NotificationEntity> SentNotifications { get; set; }
        [InverseProperty("Receiver")]
        public virtual ICollection<ReceivedNotificationEntity> ReceivedNotifications { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<User_TemplateRevisionEntity> User_TemplateRevisions { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentTemplateEntity> Vote_CommentTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentConceptEntity> Vote_CommentConcepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_FeedbackEntity> Vote_Feedbacks { get; set; }
    }
}
