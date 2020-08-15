using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("commeaf_2_leaf")]
    public partial class Commeaf_LeafEntity
    {
        [Key]
        public int LeafId { get; set; }
        [Key]
        public int CommeafId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [ForeignKey("LeafId")]
        [InverseProperty("Commeaf_Leafs")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("CommeafId")]
        [InverseProperty("Commeaf_Leafs")]
        public virtual CommeafEntity Commeaf { get; set; }
    }
}
