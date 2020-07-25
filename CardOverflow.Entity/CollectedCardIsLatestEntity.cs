using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CollectedCardIsLatestEntity
    {
        public CollectedCardIsLatestEntity()
        {
            Tag_CollectedCards = new HashSet<Tag_CollectedCardEntity>();
        }    
    
        public int Id { get; set; }
        public int UserId { get; set; }
        public int StackId { get; set; }
        public int BranchId { get; set; }
        public int BranchInstanceId { get; set; }
        public short Index { get; set; }
        public short CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        public DateTime Due { get; set; }
        public int CardSettingId { get; set; }
        public bool IsLapsed { get; set; }
        public string FrontPersonalField { get; set; }
        public string BackPersonalField { get; set; }
        public int DeckId { get; set; }
        public bool IsLatest { get; set; }
        public virtual BranchInstanceEntity BranchInstance { get; set; }
        public virtual ICollection<Tag_CollectedCardEntity> Tag_CollectedCards { get; set; }
    }
}
