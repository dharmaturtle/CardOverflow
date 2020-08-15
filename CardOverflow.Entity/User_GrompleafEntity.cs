using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("user_2_grompleaf")]
    public partial class User_GrompleafEntity
    {
        public User_GrompleafEntity()
        {
            Tag_User_Grompleafs = new HashSet<Tag_User_GrompleafEntity>();
        }

        [Key]
        public int UserId { get; set; }
        [Key]
        public int GrompleafId { get; set; }
        public int DefaultCardSettingId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [ForeignKey("GrompleafId")]
        [InverseProperty("User_Grompleafs")]
        public virtual GrompleafEntity Grompleaf { get; set; }
        [ForeignKey("DefaultCardSettingId")]
        [InverseProperty("User_Grompleafs")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_Grompleafs")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("User_Grompleaf")]
        public virtual ICollection<Tag_User_GrompleafEntity> Tag_User_Grompleafs { get; set; }
    }
}
