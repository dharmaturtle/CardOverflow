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
        }

        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int Users { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Cards")]
        public virtual UserEntity Author { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CardInstanceEntity> CardInstances { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
    }
}
