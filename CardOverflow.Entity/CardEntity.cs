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
            BranchChildren = new HashSet<CardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int Users { get; set; }
        public int? CopySourceId { get; set; }
        public int? BranchSourceId { get; set; }
        [StringLength(64)]
        public string BranchName {
            get => _BranchName;
            set {
                if (value.Length > 64) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and BranchName has a maximum length of 64. Attempted value: {value}");
                _BranchName = value;
            }
        }
        private string _BranchName;
        public bool IsListed { get; set; } = true;

        [ForeignKey("AuthorId")]
        [InverseProperty("Cards")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("BranchSourceId")]
        [InverseProperty("BranchChildren")]
        public virtual CardEntity BranchSource { get; set; }
        [ForeignKey("CopySourceId")]
        [InverseProperty("Cards")]
        public virtual CardInstanceEntity CopySource { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CardInstanceEntity> CardInstances { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("BranchSource")]
        public virtual ICollection<CardEntity> BranchChildren { get; set; }
    }
}
