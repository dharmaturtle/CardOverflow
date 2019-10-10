using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_FeedbackEntity
    {
        public int FeedbackId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("FeedbackId")]
        [InverseProperty("Vote_Feedbacks")]
        public virtual FeedbackEntity Feedback { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_Feedbacks")]
        public virtual UserEntity User { get; set; }
    }
}
