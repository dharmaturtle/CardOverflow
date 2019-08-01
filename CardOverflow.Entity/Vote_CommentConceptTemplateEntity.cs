using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentConceptTemplateEntity
    {
        public int CommentConceptTemplateId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("CommentConceptTemplateId")]
        [InverseProperty("Vote_CommentConceptTemplates")]
        public virtual CommentConceptTemplateEntity CommentConceptTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentConceptTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
