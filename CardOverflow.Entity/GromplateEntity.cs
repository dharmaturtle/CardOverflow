using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class GromplateEntity
    {
        public GromplateEntity()
        {
            TemplateRevisions = new HashSet<TemplateRevisionEntity>();
            CommentGromplates = new HashSet<CommentGromplateEntity>();
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
        [InverseProperty("Gromplates")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestId")]
        [InverseProperty("Gromplates")]
        public virtual TemplateRevisionEntity Latest { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<TemplateRevisionEntity> TemplateRevisions { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<CommentGromplateEntity> CommentGromplates { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
