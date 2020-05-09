using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentCollateEntity
    {
        [Key]
        public int CommentCollateId { get; set; }
        [Key]
        public int UserId { get; set; }

        [ForeignKey("CommentCollateId")]
        [InverseProperty("Vote_CommentCollates")]
        public virtual CommentCollateEntity CommentCollate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentCollates")]
        public virtual UserEntity User { get; set; }
    }
}
