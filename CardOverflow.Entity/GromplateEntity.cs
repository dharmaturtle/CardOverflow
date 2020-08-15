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
            Grompleafs = new HashSet<GrompleafEntity>();
            CommentGromplates = new HashSet<CommentGromplateEntity>();
            Notifications = new HashSet<NotificationEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int LatestId { get; set; }
        public bool IsListed { get; set; } = true;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Gromplates")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestId")]
        [InverseProperty("Gromplates")]
        public virtual GrompleafEntity Latest { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<GrompleafEntity> Grompleafs { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<CommentGromplateEntity> CommentGromplates { get; set; }
        [InverseProperty("Gromplate")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
