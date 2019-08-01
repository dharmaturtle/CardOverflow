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
            CommentConcepts = new HashSet<CommentConceptEntity>();
            ConceptInstances = new HashSet<ConceptInstanceEntity>();
            PublicTag_Concepts = new HashSet<PublicTag_ConceptEntity>();
            Vote_Concepts = new HashSet<Vote_ConceptEntity>();
        }

        public int Id { get; set; }
        public int MaintainerId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [ForeignKey("MaintainerId")]
        [InverseProperty("Concepts")]
        public virtual UserEntity Maintainer { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<CommentConceptEntity> CommentConcepts { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<ConceptInstanceEntity> ConceptInstances { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<PublicTag_ConceptEntity> PublicTag_Concepts { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<Vote_ConceptEntity> Vote_Concepts { get; set; }
    }
}
