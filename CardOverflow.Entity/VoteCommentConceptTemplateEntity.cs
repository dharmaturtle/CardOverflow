using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Vote_CommentConceptTemplate")]
    public partial class VoteCommentConceptTemplateEntity
    {
        public int CommentConceptTemplateId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("CommentConceptTemplateId")]
        [InverseProperty("VoteCommentConceptTemplates")]
        public virtual CommentConceptTemplateEntity CommentConceptTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("VoteCommentConceptTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
