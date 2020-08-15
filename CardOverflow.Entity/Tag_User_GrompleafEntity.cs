using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("tag_2_user_2_grompleaf")]
    public partial class Tag_User_GrompleafEntity
    {
        [Key]
        public int UserId { get; set; }
        [Key]
        public int GrompleafId { get; set; }
        [Key]
        public int DefaultTagId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [ForeignKey("DefaultTagId")]
        [InverseProperty("Tag_User_Grompleafs")]
        public virtual TagEntity DefaultTag { get; set; }
        [ForeignKey("UserId,GrompleafId")]
        [InverseProperty("Tag_User_Grompleafs")]
        public virtual User_GrompleafEntity User_Grompleaf { get; set; }
    }
}
