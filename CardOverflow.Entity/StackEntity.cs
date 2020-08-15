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
            Cards = new HashSet<CardEntity>();
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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Stacks")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("DefaultBranchId")]
        public virtual BranchEntity DefaultBranch { get; set; }
        [ForeignKey("CopySourceId")]
        [InverseProperty("StackCopySources")]
        public virtual LeafEntity CopySource { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<BranchEntity> Branches { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<CommentStackEntity> CommentStacks { get; set; }
        [InverseProperty("Stack")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
