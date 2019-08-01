using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentConceptEntity
    {
        public CommentConceptEntity()
        {
            Vote_CommentConcepts = new HashSet<Vote_CommentConceptEntity>();
        }

        public int Id { get; set; }
        public int ConceptId { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(500)]
        public string Text { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("CommentConcepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentConcepts")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentConcept")]
        public virtual ICollection<Vote_CommentConceptEntity> Vote_CommentConcepts { get; set; }
    }
}
