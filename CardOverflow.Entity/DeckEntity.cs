using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class DeckEntity : IId
    {
        public DeckEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(250)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 250) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 250. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public bool IsPublic { get; set; }
        public NpgsqlTsVector TsVector { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("Decks")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("Deck")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
    }
}
