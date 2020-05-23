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
        public int UserId { get; set; }
        [Key]
        public int StackId { get; set; }
        public int AcquiredCardId { get; set; }

        [ForeignKey("AcquiredCardId")]
        [InverseProperty("Tag_AcquiredCards")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
        [ForeignKey("TagId")]
        [InverseProperty("Tag_AcquiredCards")]
        public virtual TagEntity Tag { get; set; }
        [ForeignKey("StackId")]
        public virtual StackEntity Stack { get; set; }
    }
}
