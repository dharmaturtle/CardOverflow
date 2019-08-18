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
        public int CardId { get; set; }
        public byte Score { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Timestamp { get; set; }
        public short Interval__StepsIndexAre_32768to_32513__MinutesAre_32512to_31173__DaysAre_31172to32767 { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short TimeFromSeeingQuestionToScoreInSecondsPlus32768 { get; set; }

        [ForeignKey("UserId,CardId")]
        [InverseProperty("Histories")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
    }
}
