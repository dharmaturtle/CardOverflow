using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class User_CardTemplateInstanceEntity
    {
        public User_CardTemplateInstanceEntity()
        {
            Tag_User_CardTemplateInstances = new HashSet<Tag_User_CardTemplateInstanceEntity>();
        }

        public int UserId { get; set; }
        public int CardTemplateInstanceId { get; set; }
        public int DefaultCardOptionId { get; set; }

        [ForeignKey("CardTemplateInstanceId")]
        [InverseProperty("User_CardTemplateInstances")]
        public virtual CardTemplateInstanceEntity CardTemplateInstance { get; set; }
        [ForeignKey("DefaultCardOptionId")]
        [InverseProperty("User_CardTemplateInstances")]
        public virtual CardOptionEntity DefaultCardOption { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_CardTemplateInstances")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("User_CardTemplateInstance")]
        public virtual ICollection<Tag_User_CardTemplateInstanceEntity> Tag_User_CardTemplateInstances { get; set; }
    }
}
