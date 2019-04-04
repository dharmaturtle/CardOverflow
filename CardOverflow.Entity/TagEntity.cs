using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Tag")]
    public partial class TagEntity
    {
        public TagEntity()
        {
            ConceptTagUsers = new HashSet<ConceptTagUserEntity>();
            DeckTags = new HashSet<DeckTagEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [InverseProperty("Tag")]
        public virtual ICollection<ConceptTagUserEntity> ConceptTagUsers { get; set; }
        [InverseProperty("Tag")]
        public virtual ICollection<DeckTagEntity> DeckTags { get; set; }
    }
}
