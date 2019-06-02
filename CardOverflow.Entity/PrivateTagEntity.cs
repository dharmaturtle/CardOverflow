using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PrivateTag")]
    public partial class PrivateTagEntity
    {
        public PrivateTagEntity()
        {
            PrivateTagCards = new HashSet<PrivateTagCardEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        public string Name { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("PrivateTags")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("PrivateTag")]
        public virtual ICollection<PrivateTagCardEntity> PrivateTagCards { get; set; }
    }
}
