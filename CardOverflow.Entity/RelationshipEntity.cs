using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class RelationshipEntity
    {
        public RelationshipEntity()
        {
            Relationship_AcquiredCards = new HashSet<Relationship_AcquiredCardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int SourceId { get; set; }
        public int TargetId { get; set; }
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

        [ForeignKey("SourceId")]
        [InverseProperty("RelationshipSources")]
        public virtual CardInstanceEntity Source { get; set; }
        [ForeignKey("TargetId")]
        [InverseProperty("RelationshipTargets")]
        public virtual CardInstanceEntity Target { get; set; }
        [InverseProperty("Relationship")]
        public virtual ICollection<Relationship_AcquiredCardEntity> Relationship_AcquiredCards { get; set; }
    }
}
