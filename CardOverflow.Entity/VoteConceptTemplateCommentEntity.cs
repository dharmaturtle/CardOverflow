using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Vote_ConceptTemplateComment")]
    public partial class VoteConceptTemplateCommentEntity
    {
        public int ConceptTemplateCommentId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptTemplateCommentId")]
        [InverseProperty("VoteConceptTemplateComments")]
        public virtual ConceptTemplateCommentEntity ConceptTemplateComment { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("VoteConceptTemplateComments")]
        public virtual UserEntity User { get; set; }
    }
}
