using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("History")]
    public partial class HistoryEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CardId { get; set; }
        public byte ScoreAndMemorizationState { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Timestamp { get; set; }
        public short IntervalNegativeIsMinutesPositiveIsDays { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short TimeFromSeeingQuestionToScoreInSecondsMinus32768 { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("Histories")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Histories")]
        public virtual UserEntity User { get; set; }
    }
}
