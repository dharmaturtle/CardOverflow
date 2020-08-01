using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("vote0comment_stack")]
    public partial class Vote_CommentStackEntity
    {
        [Key]
        public int CommentStackId { get; set; }
        [Key]
        public int UserId { get; set; }

        [ForeignKey("CommentStackId")]
        [InverseProperty("Vote_CommentStacks")]
        public virtual CommentStackEntity CommentStack { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentStacks")]
        public virtual UserEntity User { get; set; }
    }
}
