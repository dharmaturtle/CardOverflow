using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Card")]
    public partial class CardEntity
    {
        public CardEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            PublicTagCards = new HashSet<PublicTagCardEntity>();
        }

        public int Id { get; set; }
        public int ConceptId { get; set; }
        public byte TemplateIndex { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("Cards")]
        public virtual ConceptEntity Concept { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<PublicTagCardEntity> PublicTagCards { get; set; }
    }
}
