using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Vote_ConceptComment")]
    public partial class VoteConceptCommentEntity
    {
        public int ConceptCommentId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptCommentId")]
        [InverseProperty("VoteConceptComments")]
        public virtual ConceptCommentEntity ConceptComment { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("VoteConceptComments")]
        public virtual UserEntity User { get; set; }
    }
}
