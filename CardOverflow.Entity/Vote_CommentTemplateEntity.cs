using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("vote_2_comment_template")]
    public partial class Vote_CommentTemplateEntity
    {
        [Key]
        public Guid CommentTemplateId { get; set; }
        [Key]
        public Guid UserId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

        [ForeignKey("CommentTemplateId")]
        [InverseProperty("Vote_CommentTemplates")]
        public virtual CommentTemplateEntity CommentTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
