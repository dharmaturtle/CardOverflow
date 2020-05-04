using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class AcquiredCardIsLatestEntity
    {
        public AcquiredCardIsLatestEntity()
        {
            Tag_AcquiredCards = new HashSet<Tag_AcquiredCardEntity>();
        }    
    
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CardId { get; set; }
        public int CardInstanceId { get; set; }
        public short CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        public DateTime Due { get; set; }
        public int CardSettingId { get; set; }
        public bool IsLapsed { get; set; }
        public bool IsLatest { get; set; }
        public virtual CardInstanceEntity CardInstance { get; set; }
        public virtual ICollection<Tag_AcquiredCardEntity> Tag_AcquiredCards { get; set; }
    }
}
