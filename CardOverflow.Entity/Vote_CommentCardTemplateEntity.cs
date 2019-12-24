using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentCardTemplateEntity
    {
        [Key]
        public int CommentCardTemplateId { get; set; }
        [Key]
        public int UserId { get; set; }

        [ForeignKey("CommentCardTemplateId")]
        [InverseProperty("Vote_CommentCardTemplates")]
        public virtual CommentCardTemplateEntity CommentCardTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentCardTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
