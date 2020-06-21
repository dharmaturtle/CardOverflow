using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class StackEntity
    {
        public StackEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            Branches = new HashSet<BranchEntity>();
            CommentStacks = new HashSet<CommentStackEntity>();
            Notifications = new HashSet<NotificationEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int Users { get; set; }
        public int? CopySourceId { get; set; }
        public int DefaultBranchId { get; set; }
        public bool IsListed { get; set; } = true;

        [ForeignKey("AuthorId")]
        [InverseProperty("Stacks")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("DefaultBranchId")]
        public virtual BranchEntity DefaultBranch { get; set; }
        [ForeignKey("CopySourceId")]
        [InverseProperty("StackCopySources")]
        public virtual BranchInstanceEntity CopySource { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<BranchEntity> Branches { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<CommentStackEntity> CommentStacks { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
