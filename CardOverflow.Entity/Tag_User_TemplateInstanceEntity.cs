using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Tag_User_TemplateInstanceEntity
    {
        [Key]
        public int UserId { get; set; }
        [Key]
        public int TemplateInstanceId { get; set; }
        [Key]
        public int DefaultTagId { get; set; }

        [ForeignKey("DefaultTagId")]
        [InverseProperty("Tag_User_TemplateInstances")]
        public virtual TagEntity DefaultTag { get; set; }
        [ForeignKey("UserId,TemplateInstanceId")]
        [InverseProperty("Tag_User_TemplateInstances")]
        public virtual User_TemplateInstanceEntity User_TemplateInstance { get; set; }
    }
}
