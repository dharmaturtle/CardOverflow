using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Tag_AcquiredCardEntity
    {
        [Key]
        public int TagId { get; set; }
        [Key]
        public int AcquiredCardId { get; set; }
        [Key]
        public int UserId { get; set; }
        [Key]
        public int CardId { get; set; }

        [ForeignKey("AcquiredCardId")]
        [InverseProperty("Tag_AcquiredCards")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
        [ForeignKey("TagId")]
        [InverseProperty("Tag_AcquiredCards")]
        public virtual TagEntity Tag { get; set; }
        [ForeignKey("CardId")]
        public virtual CardEntity Card { get; set; }
    }
}
