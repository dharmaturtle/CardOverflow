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
        public int FacetInstanceId { get; set; }
        public int CardTemplateId { get; set; }
        public byte? ClozeIndex { get; set; }

        [ForeignKey("CardTemplateId")]
        [InverseProperty("Cards")]
        public virtual CardTemplateEntity CardTemplate { get; set; }
        [ForeignKey("FacetInstanceId")]
        [InverseProperty("Cards")]
        public virtual FacetInstanceEntity FacetInstance { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
    }
}
