using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("AcquiredCard")]
    public partial class AcquiredCardEntity
    {
        public AcquiredCardEntity()
        {
            Histories = new HashSet<HistoryEntity>();
            PrivateTagAcquiredCards = new HashSet<PrivateTagAcquiredCardEntity>();
        }

        public int Id { get; set; }
        public byte MemorizationState { get; set; }
        public byte CardState { get; set; }
        public byte LapseCount { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalNegativeIsMinutesPositiveIsDays { get; set; }
        public byte? StepsIndex { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Due { get; set; }
        public int CardOptionId { get; set; }
        public int UserId { get; set; }
        public int CardId { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("AcquiredCards")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("CardOptionId")]
        [InverseProperty("AcquiredCards")]
        public virtual CardOptionEntity CardOption { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("AcquiredCards")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("AcquiredCard")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("AcquiredCard")]
        public virtual ICollection<PrivateTagAcquiredCardEntity> PrivateTagAcquiredCards { get; set; }
    }
}
