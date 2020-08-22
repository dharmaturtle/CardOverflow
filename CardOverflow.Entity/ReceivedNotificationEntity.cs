using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class ReceivedNotificationEntity
    {
        [Key]
        public Guid ReceiverId { get; set; }
        [Key]
        public Guid NotificationId { get; set; }

        [ForeignKey("NotificationId")]
        [InverseProperty("ReceivedNotifications")]
        public virtual NotificationEntity Notification { get; set; }
        [ForeignKey("ReceiverId")]
        [InverseProperty("ReceivedNotifications")]
        public virtual UserEntity Receiver { get; set; }
    }
}
