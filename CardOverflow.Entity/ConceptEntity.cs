using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Concept")]
    public partial class ConceptEntity
    {
        public ConceptEntity()
        {
            Cards = new HashSet<CardEntity>();
            ConceptTagUsers = new HashSet<ConceptTagUserEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(128)]
        public string Title { get; set; }
        [StringLength(512)]
        public string Description { get; set; }

        [InverseProperty("Concept")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<ConceptTagUserEntity> ConceptTagUsers { get; set; }
    }
}
