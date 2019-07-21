using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("CommentConcept")]
    public partial class CommentConceptEntity
    {
        public CommentConceptEntity()
        {
            VoteCommentConcepts = new HashSet<VoteCommentConceptEntity>();
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
        public virtual ICollection<VoteCommentConceptEntity> VoteCommentConcepts { get; set; }
    }
}
