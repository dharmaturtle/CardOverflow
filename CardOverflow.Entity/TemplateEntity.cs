using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class TemplateEntity
    {
        public TemplateEntity()
        {
            TemplateRevisions = new HashSet<TemplateRevisionEntity>();
            CommentTemplates = new HashSet<CommentTemplateEntity>();
            Notifications = new HashSet<NotificationEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid AuthorId { get; set; }
        public Guid LatestId { get; set; }
        public bool IsListed { get; set; } = true;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Templates")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestId")]
        [InverseProperty("Templates")]
        public virtual TemplateRevisionEntity Latest { get; set; }
        [InverseProperty("Template")]
        public virtual ICollection<TemplateRevisionEntity> TemplateRevisions { get; set; }
        [InverseProperty("Template")]
        public virtual ICollection<CommentTemplateEntity> CommentTemplates { get; set; }
        [InverseProperty("Template")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
