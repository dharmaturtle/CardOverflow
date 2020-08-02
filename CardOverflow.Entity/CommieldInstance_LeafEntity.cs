using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("commield_instance0leaf")]
    public partial class CommieldInstance_LeafEntity
    {
        [Key]
        public int LeafId { get; set; }
        [Key]
        public int CommieldInstanceId { get; set; }

        [ForeignKey("LeafId")]
        [InverseProperty("CommieldInstance_Leafs")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("CommieldInstanceId")]
        [InverseProperty("CommieldInstance_Leafs")]
        public virtual CommieldInstanceEntity CommieldInstance { get; set; }
    }
}
