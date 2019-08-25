using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class ConceptEntity
    {
        public ConceptEntity()
        {
            Facets = new HashSet<FacetEntity>();
            PublicTag_Concepts = new HashSet<PublicTag_ConceptEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 100. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public int MaintainerId { get; set; }

        [ForeignKey("MaintainerId")]
        [InverseProperty("Concepts")]
        public virtual UserEntity Maintainer { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<FacetEntity> Facets { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<PublicTag_ConceptEntity> PublicTag_Concepts { get; set; }
    }
}
