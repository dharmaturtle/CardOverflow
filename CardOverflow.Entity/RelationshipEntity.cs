using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class RelationshipEntity
    {
        public RelationshipEntity()
        {
            Relationship_Cards = new HashSet<Relationship_CardEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public NpgsqlTsVector Tsv { get; set; }

        [InverseProperty("Relationship")]
        public virtual ICollection<Relationship_CardEntity> Relationship_Cards { get; set; }
    }
}
