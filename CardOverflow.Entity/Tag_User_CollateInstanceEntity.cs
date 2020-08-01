using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("tag0user0collate_instance")]
    public partial class Tag_User_CollateInstanceEntity
    {
        [Key]
        public int UserId { get; set; }
        [Key]
        public int CollateInstanceId { get; set; }
        [Key]
        public int DefaultTagId { get; set; }

        [ForeignKey("DefaultTagId")]
        [InverseProperty("Tag_User_CollateInstances")]
        public virtual TagEntity DefaultTag { get; set; }
        [ForeignKey("UserId,CollateInstanceId")]
        [InverseProperty("Tag_User_CollateInstances")]
        public virtual User_CollateInstanceEntity User_CollateInstance { get; set; }
    }
}
