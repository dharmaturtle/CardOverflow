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
            CardOptions = new HashSet<CardOptionEntity>();
            ConceptTemplates = new HashSet<ConceptTemplateEntity>();
            Decks = new HashSet<DeckEntity>();
            Histories = new HashSet<HistoryEntity>();
            PrivateTags = new HashSet<PrivateTagEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(32)]
        public string Name { get; set; }
        [Required]
        [StringLength(254)]
        public string Email { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<CardOptionEntity> CardOptions { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<ConceptTemplateEntity> ConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<PrivateTagEntity> PrivateTags { get; set; }
    }
}
