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
            AllAcquiredCards = new HashSet<AcquiredCardEntity>();
            CardInstances = new HashSet<CardInstanceEntity>();
            CommentCards = new HashSet<CommentCardEntity>();
            BranchChildren = new HashSet<CardEntity>();
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
        [ForeignKey("BranchSourceId")]
        [InverseProperty("BranchChildren")]
        public virtual CardEntity BranchSource { get; set; }
        [ForeignKey("CopySourceId")]
        [InverseProperty("CardCopySources")]
        public virtual CardInstanceEntity CopySource { get; set; }
        [ForeignKey("LatestInstanceId")]
        [InverseProperty("CardLatestInstances")]
        public virtual CardInstanceEntity LatestInstance { get; set; }
        [InverseProperty("BranchSourceIdOrCard")]
        public virtual ICollection<AcquiredCardEntity> AllAcquiredCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<BranchEntity> Branches { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("BranchSource")]
        public virtual ICollection<CardEntity> BranchChildren { get; set; }
    }
}
