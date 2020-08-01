using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("vote0feedback")]
    public partial class Vote_FeedbackEntity
    {
        [Key]
        public int FeedbackId { get; set; }
        [Key]
        public int UserId { get; set; }

        [ForeignKey("FeedbackId")]
        [InverseProperty("Vote_Feedbacks")]
        public virtual FeedbackEntity Feedback { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_Feedbacks")]
        public virtual UserEntity User { get; set; }
    }
}
