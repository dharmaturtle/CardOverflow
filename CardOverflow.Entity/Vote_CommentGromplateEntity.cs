using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("vote0comment_gromplate")]
    public partial class Vote_CommentGromplateEntity
    {
        [Key]
        public int CommentGromplateId { get; set; }
        [Key]
        public int UserId { get; set; }

        [ForeignKey("CommentGromplateId")]
        [InverseProperty("Vote_CommentGromplates")]
        public virtual CommentGromplateEntity CommentGromplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentGromplates")]
        public virtual UserEntity User { get; set; }
    }
}
