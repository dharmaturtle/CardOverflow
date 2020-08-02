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
            Cards = new HashSet<CardEntity>();
            DeckFollowers = new HashSet<DeckFollowerEntity>();
            DerivedDecks = new HashSet<DeckEntity>();
            Notifications = new HashSet<NotificationEntity>();
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
        public int? SourceId { get; set; }
        public int Followers { get; set; }

        [ForeignKey("SourceId")]
        [InverseProperty("DerivedDecks")]
        public virtual DeckEntity Source { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Decks")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("Deck")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Deck")]
        public virtual ICollection<DeckFollowerEntity> DeckFollowers { get; set; }
        [InverseProperty("Source")]
        public virtual ICollection<DeckEntity> DerivedDecks { get; set; }
        [InverseProperty("Deck")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
