using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("vote_2_comment_concept")]
    public partial class Vote_CommentConceptEntity
    {
        [Key]
        public Guid CommentConceptId { get; set; }
        [Key]
        public Guid UserId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

        [ForeignKey("CommentConceptId")]
        [InverseProperty("Vote_CommentConcepts")]
        public virtual CommentConceptEntity CommentConcept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentConcepts")]
        public virtual UserEntity User { get; set; }
    }
}
