using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PublicTag_Card")]
    public partial class PublicTagCardEntity
    {
        public int PublicTagId { get; set; }
        public int CardId { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("PublicTagCards")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("PublicTagId")]
        [InverseProperty("PublicTagCards")]
        public virtual PublicTagEntity PublicTag { get; set; }
    }
}
