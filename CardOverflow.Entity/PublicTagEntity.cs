using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PublicTag")]
    public partial class PublicTagEntity
    {
        public PublicTagEntity()
        {
            PublicTagCards = new HashSet<PublicTagCardEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        public string Name { get; set; }

        [InverseProperty("PublicTag")]
        public virtual ICollection<PublicTagCardEntity> PublicTagCards { get; set; }
    }
}
