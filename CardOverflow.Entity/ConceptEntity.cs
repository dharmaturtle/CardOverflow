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
            CommentConcepts = new HashSet<CommentConceptEntity>();
            ConceptInstances = new HashSet<ConceptInstanceEntity>();
            PublicTagConcepts = new HashSet<PublicTagConceptEntity>();
            VoteConcepts = new HashSet<VoteConceptEntity>();
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
        public virtual ICollection<PublicTagConceptEntity> PublicTagConcepts { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<VoteConceptEntity> VoteConcepts { get; set; }
    }
}
