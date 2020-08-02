using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("tag0user0gromplate_instance")]
    public partial class Tag_User_GromplateInstanceEntity
    {
        [Key]
        public int UserId { get; set; }
        [Key]
        public int GromplateInstanceId { get; set; }
        [Key]
        public int DefaultTagId { get; set; }

        [ForeignKey("DefaultTagId")]
        [InverseProperty("Tag_User_GromplateInstances")]
        public virtual TagEntity DefaultTag { get; set; }
        [ForeignKey("UserId,GromplateInstanceId")]
        [InverseProperty("Tag_User_GromplateInstances")]
        public virtual User_GromplateInstanceEntity User_GromplateInstance { get; set; }
    }
}
