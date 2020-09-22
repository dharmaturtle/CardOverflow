using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("vote_2_comment_stack")]
    public partial class Vote_CommentStackEntity
    {
        [Key]
        public Guid CommentStackId { get; set; }
        [Key]
        public Guid UserId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

        [ForeignKey("CommentStackId")]
        [InverseProperty("Vote_CommentStacks")]
        public virtual CommentStackEntity CommentStack { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentStacks")]
        public virtual UserEntity User { get; set; }
    }
}
