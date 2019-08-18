using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class AcquiredCardEntity
    {
        public AcquiredCardEntity()
        {
            Histories = new HashSet<HistoryEntity>();
            PrivateTag_AcquiredCards = new HashSet<PrivateTag_AcquiredCardEntity>();
        }

        public int UserId { get; set; }
        public int CardId { get; set; }
        public byte CardState { get; set; }
        public byte LapseCount { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short Interval__StepsIndexAre_32768to_32513__MinutesAre_32512to_31173__DaysAre_31172to32767 { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Due { get; set; }
        public int CardOptionId { get; set; }

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
        public virtual ICollection<PrivateTag_AcquiredCardEntity> PrivateTag_AcquiredCards { get; set; }
    }
}
