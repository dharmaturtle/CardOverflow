using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("communal_field_instance0branch_instance")]
    public partial class CommunalFieldInstance_BranchInstanceEntity
    {
        [Key]
        public int BranchInstanceId { get; set; }
        [Key]
        public int CommunalFieldInstanceId { get; set; }

        [ForeignKey("BranchInstanceId")]
        [InverseProperty("CommunalFieldInstance_BranchInstances")]
        public virtual BranchInstanceEntity BranchInstance { get; set; }
        [ForeignKey("CommunalFieldInstanceId")]
        [InverseProperty("CommunalFieldInstance_BranchInstances")]
        public virtual CommunalFieldInstanceEntity CommunalFieldInstance { get; set; }
    }
}
