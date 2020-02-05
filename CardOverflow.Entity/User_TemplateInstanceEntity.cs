using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class User_TemplateInstanceEntity
    {
        public User_TemplateInstanceEntity()
        {
            Tag_User_TemplateInstances = new HashSet<Tag_User_TemplateInstanceEntity>();
        }

        [Key]
        public int UserId { get; set; }
        [Key]
        public int TemplateInstanceId { get; set; }
        public int DefaultCardSettingId { get; set; }

        [ForeignKey("DefaultCardSettingId")]
        [InverseProperty("User_TemplateInstances")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("TemplateInstanceId")]
        [InverseProperty("User_TemplateInstances")]
        public virtual TemplateInstanceEntity TemplateInstance { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_TemplateInstances")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("User_TemplateInstance")]
        public virtual ICollection<Tag_User_TemplateInstanceEntity> Tag_User_TemplateInstances { get; set; }
    }
}
