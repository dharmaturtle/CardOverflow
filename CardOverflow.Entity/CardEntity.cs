using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardEntity
    {
        public CardEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            Branches = new HashSet<BranchEntity>();
            CommentCards = new HashSet<CommentCardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int Users { get; set; }
        public int? CopySourceId { get; set; }
        public bool IsListed { get; set; } = true;
        public int DefaultBranchId { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Cards")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("CopySourceId")]
        [InverseProperty("CardCopySources")]
        public virtual BranchInstanceEntity CopySource { get; set; }
        [ForeignKey("DefaultBranchId")]
        [InverseProperty("Cards")]
        public virtual BranchEntity DefaultBranch { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<BranchEntity> Branches { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
    }
}
