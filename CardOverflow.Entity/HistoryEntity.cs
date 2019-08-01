using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class HistoryEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ConceptInstanceId { get; set; }
        public int CardTemplateId { get; set; }
        public byte Score { get; set; }
        public byte MemorizationState { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Timestamp { get; set; }
        public short IntervalNegativeIsMinutesPositiveIsDays { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short TimeFromSeeingQuestionToScoreInSecondsMinus32768 { get; set; }

        [ForeignKey("UserId,ConceptInstanceId,CardTemplateId")]
        [InverseProperty("Histories")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
    }
}
