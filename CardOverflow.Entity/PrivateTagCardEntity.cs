using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PrivateTag_Card")]
    public partial class PrivateTagCardEntity
    {
        public int PrivateTagId { get; set; }
        public int CardId { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("PrivateTagCards")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("PrivateTagId")]
        [InverseProperty("PrivateTagCards")]
        public virtual PrivateTagEntity PrivateTag { get; set; }
    }
}
