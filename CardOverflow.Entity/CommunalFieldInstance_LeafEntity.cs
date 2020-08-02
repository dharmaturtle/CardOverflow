using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("communal_field_instance0leaf")]
    public partial class CommunalFieldInstance_LeafEntity
    {
        [Key]
        public int LeafId { get; set; }
        [Key]
        public int CommunalFieldInstanceId { get; set; }

        [ForeignKey("LeafId")]
        [InverseProperty("CommunalFieldInstance_Leafs")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("CommunalFieldInstanceId")]
        [InverseProperty("CommunalFieldInstance_Leafs")]
        public virtual CommunalFieldInstanceEntity CommunalFieldInstance { get; set; }
    }
}
