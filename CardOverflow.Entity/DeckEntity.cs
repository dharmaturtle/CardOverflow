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
        public string Name {
            get => _Name;
            set {
                if (value.Length > 128) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 128. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public int UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string Query {
            get => _Query;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Query has a maximum length of 100. Attempted value: {value}");
                _Query = value;
            }
        }
        private string _Query;

        [ForeignKey("UserId")]
        [InverseProperty("Decks")]
        public virtual UserEntity User { get; set; }
    }
}
