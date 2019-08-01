using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentConceptEntity
    {
        public int CommentConceptId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("CommentConceptId")]
        [InverseProperty("Vote_CommentConcepts")]
        public virtual CommentConceptEntity CommentConcept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentConcepts")]
        public virtual UserEntity User { get; set; }
    }
}
