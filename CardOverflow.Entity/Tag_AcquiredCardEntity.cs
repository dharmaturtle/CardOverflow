using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Tag_AcquiredCardEntity
    {
        public int TagId { get; set; }
        public int AcquiredCardId { get; set; }

        [ForeignKey("AcquiredCardId")]
        [InverseProperty("Tag_AcquiredCards")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
        [ForeignKey("TagId")]
        [InverseProperty("Tag_AcquiredCards")]
        public virtual TagEntity Tag { get; set; }
    }
}
