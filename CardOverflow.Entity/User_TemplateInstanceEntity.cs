using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class User_CollateInstanceEntity
    {
        public User_CollateInstanceEntity()
        {
            Tag_User_CollateInstances = new HashSet<Tag_User_CollateInstanceEntity>();
        }

        [Key]
        public int UserId { get; set; }
        [Key]
        public int CollateInstanceId { get; set; }
        public int DefaultCardSettingId { get; set; }

        [ForeignKey("DefaultCardSettingId")]
        [InverseProperty("User_CollateInstances")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("CollateInstanceId")]
        [InverseProperty("User_CollateInstances")]
        public virtual CollateInstanceEntity CollateInstance { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_CollateInstances")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("User_CollateInstance")]
        public virtual ICollection<Tag_User_CollateInstanceEntity> Tag_User_CollateInstances { get; set; }
    }
}
