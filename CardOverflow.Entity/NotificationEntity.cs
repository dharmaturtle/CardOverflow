using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class NotificationEntity
    {
        public NotificationEntity()
        {
            ReceivedNotifications = new HashSet<ReceivedNotificationEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int SenderId { get; set; }
        public DateTime TimeStamp { get; set; }
        public NotificationType Type { get; set; }
        [StringLength(4000)]
        public string Message {
            get => _Message;
            set {
                if (value?.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Message has a maximum length of 4000. Attempted value: {value}");
                _Message = value?.Replace("\0", string.Empty);
            }
        }
        private string _Message;
        public int? StackId { get; set; }
        public int? BranchId { get; set; }
        public int? BranchInstanceId { get; set; }
        public int? DeckId { get; set; }
        public int? CollateId { get; set; }
        public int? CollateInstanceId { get; set; }

        [ForeignKey("BranchId")]
        [InverseProperty("NotificationBranches")]
        public virtual BranchEntity Branch { get; set; }
        [ForeignKey("BranchInstanceId")]
        [InverseProperty("NotificationBranchInstances")]
        public virtual BranchInstanceEntity BranchInstance { get; set; }
        [ForeignKey("CollateId")]
        [InverseProperty("Notifications")]
        public virtual CollateEntity Collate { get; set; }
        [ForeignKey("CollateInstanceId")]
        [InverseProperty("Notifications")]
        public virtual CollateInstanceEntity CollateInstance { get; set; }
        [ForeignKey("DeckId")]
        [InverseProperty("Notifications")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("SenderId")]
        [InverseProperty("SentNotifications")]
        public virtual UserEntity Sender { get; set; }
        [ForeignKey("StackId")]
        [InverseProperty("Notifications")]
        public virtual StackEntity Stack { get; set; }
        [InverseProperty("Notification")]
        public virtual ICollection<ReceivedNotificationEntity> ReceivedNotifications { get; set; }
    }
}
