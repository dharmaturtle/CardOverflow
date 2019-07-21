using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Vote_CommentConcept")]
    public partial class VoteCommentConceptEntity
    {
        public int CommentConceptId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("CommentConceptId")]
        [InverseProperty("VoteCommentConcepts")]
        public virtual CommentConceptEntity CommentConcept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("VoteCommentConcepts")]
        public virtual UserEntity User { get; set; }
    }
}
