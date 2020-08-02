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
        public int? CollectedCardId { get; set; }
        public int UserId { get; set; }
        public int? LeafId { get; set; }
        public short Index { get; set; }
        public short Score { get; set; }
        public DateTime Timestamp { get; set; }
        public short IntervalWithUnusedStepsIndex { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short TimeFromSeeingQuestionToScoreInSecondsPlus32768 { get; set; }

        [ForeignKey("CollectedCardId")]
        [InverseProperty("Histories")]
        public virtual CollectedCardEntity CollectedCard { get; set; }
        [ForeignKey("LeafId")]
        [InverseProperty("Histories")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Histories")]
        public virtual UserEntity User { get; set; }
    }
}
