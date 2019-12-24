using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentCardEntity
    {
        [Key]
        public int CommentCardId { get; set; }
        [Key]
        public int UserId { get; set; }

        [ForeignKey("CommentCardId")]
        [InverseProperty("Vote_CommentCards")]
        public virtual CommentCardEntity CommentCard { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentCards")]
        public virtual UserEntity User { get; set; }
    }
}
