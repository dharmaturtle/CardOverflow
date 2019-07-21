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
        }

        public int ConceptInstanceId { get; set; }
        public int CardTemplateId { get; set; }

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
