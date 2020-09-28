using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

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
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid AuthorId { get; set; }
        public int Users { get; set; }
        public Guid? CopySourceId { get; set; }
        public Guid DefaultBranchId { get; set; }
        public bool IsListed { get; set; } = true;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        [Required]
        [StringLength(300)]
        public string[] Tags { get; set; } = new string[0];
        [Required]
        public int[] TagsCount { get; set; } = new int[0];

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
