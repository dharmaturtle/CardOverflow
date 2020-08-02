using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class GromplateEntity
    {
        public GromplateEntity()
        {
            GromplateInstances = new HashSet<GromplateInstanceEntity>();
            CommentGromplates = new HashSet<CommentGromplateEntity>();
            Notifications = new HashSet<NotificationEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int LatestInstanceId { get; set; }
        public bool IsListed { get; set; } = true;

        [ForeignKey("AuthorId")]
        [InverseProperty("Gromplates")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestInstanceId")]
        [InverseProperty("Gromplates")]
        public virtual GromplateInstanceEntity LatestInstance { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<GromplateInstanceEntity> GromplateInstances { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<CommentGromplateEntity> CommentGromplates { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
