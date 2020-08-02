using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CardOverflow.Pure;

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
        public int? LeafId { get; set; }
        public int? DeckId { get; set; }
        public int? GromplateId { get; set; }
        public int? GrompleafId { get; set; }

        [ForeignKey("BranchId")]
        [InverseProperty("NotificationBranches")]
        public virtual BranchEntity Branch { get; set; }
        [ForeignKey("LeafId")]
        [InverseProperty("NotificationLeafs")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("GromplateId")]
        [InverseProperty("Notifications")]
        public virtual GromplateEntity Gromplate { get; set; }
        [ForeignKey("GrompleafId")]
        [InverseProperty("Notifications")]
        public virtual GrompleafEntity Grompleaf { get; set; }
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
