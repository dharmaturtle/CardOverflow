using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("vote_2_feedback")]
    public partial class Vote_FeedbackEntity
    {
        [Key]
        public Guid FeedbackId { get; set; }
        [Key]
        public Guid UserId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [ForeignKey("FeedbackId")]
        [InverseProperty("Vote_Feedbacks")]
        public virtual FeedbackEntity Feedback { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_Feedbacks")]
        public virtual UserEntity User { get; set; }
    }
}
