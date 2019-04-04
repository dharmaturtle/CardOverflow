using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Deck_Tag")]
    public partial class DeckTagEntity
    {
        public int DeckId { get; set; }
        public int TagId { get; set; }

        [ForeignKey("DeckId")]
        [InverseProperty("DeckTags")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("TagId")]
        [InverseProperty("DeckTags")]
        public virtual TagEntity Tag { get; set; }
    }
}
