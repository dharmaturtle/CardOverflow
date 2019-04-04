using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Deck_Card")]
    public partial class DeckCardEntity
    {
        public int DeckId { get; set; }
        public int CardId { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("DeckCards")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("DeckId")]
        [InverseProperty("DeckCards")]
        public virtual DeckEntity Deck { get; set; }
    }
}
