using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Deck")]
    public partial class DeckEntity
    {
        public DeckEntity()
        {
            DeckCards = new HashSet<DeckCardEntity>();
            DeckTags = new HashSet<DeckTagEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(128)]
        public string Name { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("Decks")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("Deck")]
        public virtual ICollection<DeckCardEntity> DeckCards { get; set; }
        [InverseProperty("Deck")]
        public virtual ICollection<DeckTagEntity> DeckTags { get; set; }
    }
}
