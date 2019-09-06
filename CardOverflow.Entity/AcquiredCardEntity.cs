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
            Tag_AcquiredCards = new HashSet<Tag_AcquiredCardEntity>();
        }

        public int UserId { get; set; }
        public int CardInstanceId { get; set; }
        public byte CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Due { get; set; }
        public int CardOptionId { get; set; }
        public bool IsLapsed { get; set; }

        [ForeignKey("CardInstanceId")]
        [InverseProperty("AcquiredCards")]
        public virtual CardInstanceEntity CardInstance { get; set; }
        [ForeignKey("CardOptionId")]
        [InverseProperty("AcquiredCards")]
        public virtual CardOptionEntity CardOption { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("AcquiredCards")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("AcquiredCard")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("AcquiredCard")]
        public virtual ICollection<Tag_AcquiredCardEntity> Tag_AcquiredCards { get; set; }
    }
}
