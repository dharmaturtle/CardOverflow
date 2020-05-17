using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Relationship_AcquiredCardEntity
    {
        [Key]
        public int RelationshipId { get; set; }
        [Key]
        public int UserId { get; set; }
        [Key]
        public int SourceCardId { get; set; }
        [Key]
        public int TargetCardId { get; set; }
        public int SourceAcquiredCardId { get; set; }
        public int TargetAcquiredCardId { get; set; }

        [ForeignKey("RelationshipId")]
        [InverseProperty("Relationship_AcquiredCards")]
        public virtual RelationshipEntity Relationship { get; set; }
        [ForeignKey("SourceAcquiredCardId")]
        [InverseProperty("Relationship_AcquiredCardSourceAcquiredCards")]
        public virtual AcquiredCardEntity SourceAcquiredCard { get; set; }
        [ForeignKey("TargetAcquiredCardId")]
        [InverseProperty("Relationship_AcquiredCardTargetAcquiredCards")]
        public virtual AcquiredCardEntity TargetAcquiredCard { get; set; }
    }
}
