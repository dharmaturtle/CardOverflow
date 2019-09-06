using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Tag_User_CardTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int CardTemplateInstanceId { get; set; }
        public int DefaultTagId { get; set; }

        [ForeignKey("DefaultTagId")]
        [InverseProperty("Tag_User_CardTemplateInstances")]
        public virtual TagEntity DefaultTag { get; set; }
        [ForeignKey("UserId,CardTemplateInstanceId")]
        [InverseProperty("Tag_User_CardTemplateInstances")]
        public virtual User_CardTemplateInstanceEntity User_CardTemplateInstance { get; set; }
    }
}
