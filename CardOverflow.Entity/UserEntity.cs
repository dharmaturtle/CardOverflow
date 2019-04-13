using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("User")]
    public partial class UserEntity
    {
        public UserEntity()
        {
            ConceptOptions = new HashSet<ConceptOptionEntity>();
            ConceptTagUsers = new HashSet<ConceptTagUserEntity>();
            Decks = new HashSet<DeckEntity>();
            Histories = new HashSet<HistoryEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(32)]
        public string Name { get; set; }
        [Required]
        [StringLength(254)]
        public string Email { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<ConceptOptionEntity> ConceptOptions { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<ConceptTagUserEntity> ConceptTagUsers { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
    }
}
