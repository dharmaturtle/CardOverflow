using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentTemplateEntity
    {
        [Key]
        public int CommentTemplateId { get; set; }
        [Key]
        public int UserId { get; set; }

        [ForeignKey("CommentTemplateId")]
        [InverseProperty("Vote_CommentTemplates")]
        public virtual CommentTemplateEntity CommentTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
