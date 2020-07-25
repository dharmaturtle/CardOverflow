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
            CollectedCards = new HashSet<CollectedCardEntity>();
            Branches = new HashSet<BranchEntity>();
            CardSettings = new HashSet<CardSettingEntity>();
            Collates = new HashSet<CollateEntity>();
            CommentCollates = new HashSet<CommentCollateEntity>();
            CommentStacks = new HashSet<CommentStackEntity>();
            CommunalFields = new HashSet<CommunalFieldEntity>();
            DeckFollowers = new HashSet<DeckFollowersEntity>();
            Decks = new HashSet<DeckEntity>();
            Feedbacks = new HashSet<FeedbackEntity>();
            Filters = new HashSet<FilterEntity>();
            Histories = new HashSet<HistoryEntity>();
            SentNotifications = new HashSet<NotificationEntity>();
            ReceivedNotifications = new HashSet<ReceivedNotificationEntity>();
            Stacks = new HashSet<StackEntity>();
            User_CollateInstances = new HashSet<User_CollateInstanceEntity>();
            Vote_CommentCollates = new HashSet<Vote_CommentCollateEntity>();
            Vote_CommentStacks = new HashSet<Vote_CommentStackEntity>();
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
        public int DefaultCardSettingId { get; set; }
        public int DefaultDeckId { get; set; }
        public bool ShowNextReviewTime { get; set; }
        public bool ShowRemainingCardCount { get; set; }
        public short MixNewAndReview { get; set; }
        public short NextDayStartsAtXHoursPastMidnight { get; set; }
        public short LearnAheadLimitInMinutes { get; set; }
        public short TimeboxTimeLimitInMinutes { get; set; }
        public bool IsNightMode { get; set; }

        [ForeignKey("DefaultCardSettingId")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("DefaultDeckId")]
        public virtual DeckEntity DefaultDeck { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CollectedCardEntity> CollectedCards { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<BranchEntity> Branches { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CardSettingEntity> CardSettings { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<CollateEntity> Collates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentCollateEntity> CommentCollates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentStackEntity> CommentStacks { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<CommunalFieldEntity> CommunalFields { get; set; }
        [InverseProperty("Follower")]
        public virtual ICollection<DeckFollowersEntity> DeckFollowers { get; set; }
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
        public virtual ICollection<User_CollateInstanceEntity> User_CollateInstances { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentCollateEntity> Vote_CommentCollates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentStackEntity> Vote_CommentStacks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_FeedbackEntity> Vote_Feedbacks { get; set; }
    }
}
