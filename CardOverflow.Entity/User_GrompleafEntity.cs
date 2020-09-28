using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("user_2_grompleaf")]
    public partial class User_GrompleafEntity
    {
        [Key]
        public Guid UserId { get; set; }
        [Key]
        public Guid GrompleafId { get; set; }
        public Guid DefaultCardSettingId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        [Required]
        [StringLength(300)]
        public string[] DefaultTags { get; set; } = new string[0];

        [ForeignKey("GrompleafId")]
        [InverseProperty("User_Grompleafs")]
        public virtual GrompleafEntity Grompleaf { get; set; }
        [ForeignKey("DefaultCardSettingId")]
        [InverseProperty("User_Grompleafs")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_Grompleafs")]
        public virtual UserEntity User { get; set; }
    }
}
