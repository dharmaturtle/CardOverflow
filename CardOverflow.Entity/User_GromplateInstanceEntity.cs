using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("user0gromplate_instance")]
    public partial class User_GromplateInstanceEntity
    {
        public User_GromplateInstanceEntity()
        {
            Tag_User_GromplateInstances = new HashSet<Tag_User_GromplateInstanceEntity>();
        }

        [Key]
        public int UserId { get; set; }
        [Key]
        public int GromplateInstanceId { get; set; }
        public int DefaultCardSettingId { get; set; }

        [ForeignKey("GromplateInstanceId")]
        [InverseProperty("User_GromplateInstances")]
        public virtual GromplateInstanceEntity GromplateInstance { get; set; }
        [ForeignKey("DefaultCardSettingId")]
        [InverseProperty("User_GromplateInstances")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_GromplateInstances")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("User_GromplateInstance")]
        public virtual ICollection<Tag_User_GromplateInstanceEntity> Tag_User_GromplateInstances { get; set; }
    }
}
