using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class HistoryEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid? CardId { get; set; }
        public Guid UserId { get; set; }
        public Guid? LeafId { get; set; }
        public short Index { get; set; }
        public short Score { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public short IntervalWithUnusedStepsIndex { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short TimeFromSeeingQuestionToScoreInSecondsPlus32768 { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("Histories")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("LeafId")]
        [InverseProperty("Histories")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Histories")]
        public virtual UserEntity User { get; set; }
    }
}
