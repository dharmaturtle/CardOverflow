using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;

namespace CardOverflow.Entity
{
    [Table("padawan")]
    public partial class UserEntity
    {
        public UserEntity()
        {
            Cards = new HashSet<CardEntity>();
            Branches = new HashSet<BranchEntity>();
            CardSettings = new HashSet<CardSettingEntity>();
            Gromplates = new HashSet<GromplateEntity>();
            CommentGromplates = new HashSet<CommentGromplateEntity>();
            CommentStacks = new HashSet<CommentStackEntity>();
            Commields = new HashSet<CommieldEntity>();
            DeckFollowers = new HashSet<DeckFollowerEntity>();
            Decks = new HashSet<DeckEntity>();
            Feedbacks = new HashSet<FeedbackEntity>();
            Filters = new HashSet<FilterEntity>();
            Histories = new HashSet<HistoryEntity>();
            SentNotifications = new HashSet<NotificationEntity>();
            ReceivedNotifications = new HashSet<ReceivedNotificationEntity>();
            Stacks = new HashSet<StackEntity>();
            User_Grompleafs = new HashSet<User_GrompleafEntity>();
            Vote_CommentGromplates = new HashSet<Vote_CommentGromplateEntity>();
            Vote_CommentStacks = new HashSet<Vote_CommentStackEntity>();
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
        public short MixNewAndReview { get; set; }
        public short NextDayStartsAtXHoursPastMidnight { get; set; }
        public short LearnAheadLimitInMinutes { get; set; }
        public short TimeboxTimeLimitInMinutes { get; set; }
        public bool IsNightMode { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }

        [ForeignKey("DefaultCardSettingId")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("DefaultDeckId")]
        public virtual DeckEntity DefaultDeck { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<BranchEntity> Branches { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CardSettingEntity> CardSettings { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<GromplateEntity> Gromplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentGromplateEntity> CommentGromplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentStackEntity> CommentStacks { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<CommieldEntity> Commields { get; set; }
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
        public virtual ICollection<StackEntity> Stacks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<User_GrompleafEntity> User_Grompleafs { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentGromplateEntity> Vote_CommentGromplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentStackEntity> Vote_CommentStacks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_FeedbackEntity> Vote_Feedbacks { get; set; }
    }
}
