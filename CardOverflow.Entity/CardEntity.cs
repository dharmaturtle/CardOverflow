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
            CardInstances = new HashSet<CardInstanceEntity>();
            CommentCards = new HashSet<CommentCardEntity>();
            VariantChildren = new HashSet<CardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int Users { get; set; }
        public int? ForkParentId { get; set; }
        public int? VariantParentId { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Cards")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("ForkParentId")]
        [InverseProperty("Cards")]
        public virtual CardInstanceEntity ForkParent { get; set; }
        [ForeignKey("VariantParentId")]
        [InverseProperty("VariantChildren")]
        public virtual CardEntity VariantParent { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CardInstanceEntity> CardInstances { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("VariantParent")]
        public virtual ICollection<CardEntity> VariantChildren { get; set; }
    }
}
