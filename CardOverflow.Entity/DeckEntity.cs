using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class DeckEntity
    {
        public int Id { get; set; }
        [Required]
        [StringLength(128)]
        public string Name { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string Query { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("Decks")]
        public virtual UserEntity User { get; set; }
    }
}
