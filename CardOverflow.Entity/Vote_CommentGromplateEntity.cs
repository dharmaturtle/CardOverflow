using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("vote_2_comment_gromplate")]
    public partial class Vote_CommentGromplateEntity
    {
        [Key]
        public Guid CommentGromplateId { get; set; }
        [Key]
        public Guid UserId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

        [ForeignKey("CommentGromplateId")]
        [InverseProperty("Vote_CommentGromplates")]
        public virtual CommentGromplateEntity CommentGromplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentGromplates")]
        public virtual UserEntity User { get; set; }
    }
}
