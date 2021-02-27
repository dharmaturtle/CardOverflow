using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class CardIsLatestEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ConceptId { get; set; }
        public Guid BranchId { get; set; }
        public Guid LeafId { get; set; }
        public short Index { get; set; }
        public short CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        public Instant Due { get; set; }
        public Guid CardSettingId { get; set; }
        public bool IsLapsed { get; set; }
        public string FrontPersonalField { get; set; }
        public string BackPersonalField { get; set; }
        public Guid DeckId { get; set; }
        public bool IsLatest { get; set; }
        public string[] Tags { get; set; }
        public virtual LeafEntity Leaf { get; set; }
    }
}
