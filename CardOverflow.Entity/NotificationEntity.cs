using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CardOverflow.Pure;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class NotificationEntity
    {
        public NotificationEntity()
        {
            ReceivedNotifications = new HashSet<ReceivedNotificationEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid SenderId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
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
        public Guid? ConceptId { get; set; }
        public Guid? ExampleId { get; set; }
        public Guid? LeafId { get; set; }
        public Guid? DeckId { get; set; }
        public Guid? TemplateId { get; set; }
        public Guid? TemplateRevisionId { get; set; }

        [ForeignKey("ExampleId")]
        [InverseProperty("NotificationExamples")]
        public virtual ExampleEntity Example { get; set; }
        [ForeignKey("LeafId")]
        [InverseProperty("NotificationLeafs")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("TemplateId")]
        [InverseProperty("Notifications")]
        public virtual TemplateEntity Template { get; set; }
        [ForeignKey("TemplateRevisionId")]
        [InverseProperty("Notifications")]
        public virtual TemplateRevisionEntity TemplateRevision { get; set; }
        [ForeignKey("DeckId")]
        [InverseProperty("Notifications")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("SenderId")]
        [InverseProperty("SentNotifications")]
        public virtual UserEntity Sender { get; set; }
        [ForeignKey("ConceptId")]
        [InverseProperty("Notifications")]
        public virtual ConceptEntity Concept { get; set; }
        [InverseProperty("Notification")]
        public virtual ICollection<ReceivedNotificationEntity> ReceivedNotifications { get; set; }
    }
}
