using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class HistoryEntity
    {
        [Key]
        public long Id { get; set; }
        public int? AcquiredCardId { get; set; }
        public int UserId { get; set; }
        public int BranchInstanceId { get; set; }
        public short Index { get; set; }
        public short Score { get; set; }
        public DateTime Timestamp { get; set; }
        public short IntervalWithUnusedStepsIndex { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short TimeFromSeeingQuestionToScoreInSecondsPlus32768 { get; set; }
    
        [ForeignKey("AcquiredCardId")]
        [InverseProperty("Histories")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Histories")]
        public virtual UserEntity User { get; set; }
        [ForeignKey("BranchInstanceId")]
        public virtual BranchInstanceEntity BranchInstance { get; set; }
    }
}
