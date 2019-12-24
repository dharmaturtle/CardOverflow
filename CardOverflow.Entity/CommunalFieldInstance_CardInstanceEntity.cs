using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommunalFieldInstance_CardInstanceEntity
    {
        [Key]
        public int CardInstanceId { get; set; }
        [Key]
        public int CommunalFieldInstanceId { get; set; }

        [ForeignKey("CardInstanceId")]
        [InverseProperty("CommunalFieldInstance_CardInstances")]
        public virtual CardInstanceEntity CardInstance { get; set; }
        [ForeignKey("CommunalFieldInstanceId")]
        [InverseProperty("CommunalFieldInstance_CardInstances")]
        public virtual CommunalFieldInstanceEntity CommunalFieldInstance { get; set; }
    }
}
