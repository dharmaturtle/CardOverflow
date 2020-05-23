using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CollateEntity
    {
        public CollateEntity()
        {
            CollateInstances = new HashSet<CollateInstanceEntity>();
            CommentCollates = new HashSet<CommentCollateEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int LatestInstanceId { get; set; }
        public bool IsListed { get; set; } = true;

        [ForeignKey("AuthorId")]
        [InverseProperty("Collates")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestInstanceId")]
        [InverseProperty("Collates")]
        public virtual CollateInstanceEntity LatestInstance { get; set; }
        [InverseProperty("Collate")]
        public virtual ICollection<CollateInstanceEntity> CollateInstances { get; set; }
        [InverseProperty("Collate")]
        public virtual ICollection<CommentCollateEntity> CommentCollates { get; set; }
    }
}
