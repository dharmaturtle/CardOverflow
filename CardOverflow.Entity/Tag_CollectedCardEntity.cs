using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Tag_CollectedCardEntity
    {
        [Key]
        public int TagId { get; set; }
        [Key]
        public int UserId { get; set; }
        [Key]
        public int StackId { get; set; }
        public int CollectedCardId { get; set; }

        [ForeignKey("CollectedCardId")]
        [InverseProperty("Tag_CollectedCards")]
        public virtual CollectedCardEntity CollectedCard { get; set; }
        [ForeignKey("TagId")]
        [InverseProperty("Tag_CollectedCards")]
        public virtual TagEntity Tag { get; set; }
        [ForeignKey("StackId")]
        public virtual StackEntity Stack { get; set; }
    }
}
