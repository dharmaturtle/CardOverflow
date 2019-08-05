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
        }

        public int Id { get; set; }
        public int ConceptInstanceId { get; set; }
        public int CardTemplateId { get; set; }
        public byte? ClozeIndex { get; set; }

        [ForeignKey("CardTemplateId")]
        [InverseProperty("Cards")]
        public virtual CardTemplateEntity CardTemplate { get; set; }
        [ForeignKey("ConceptInstanceId")]
        [InverseProperty("Cards")]
        public virtual ConceptInstanceEntity ConceptInstance { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
    }
}
