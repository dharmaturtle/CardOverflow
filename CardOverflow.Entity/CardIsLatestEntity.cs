using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardIsLatestEntity
    {
        public CardIsLatestEntity()
        {
            Tag_Cards = new HashSet<Tag_CardEntity>();
        }    
    
        public int Id { get; set; }
        public int UserId { get; set; }
        public int StackId { get; set; }
        public int BranchId { get; set; }
        public int LeafId { get; set; }
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
        public virtual LeafEntity Leaf { get; set; }
        public virtual ICollection<Tag_CardEntity> Tag_Cards { get; set; }
    }
}
