using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Relationship_AcquiredCardEntity
    {
        [Key]
        public int SourceAcquiredCardId { get; set; }
        [Key]
        public int TargetAcquiredCardId { get; set; }
        [Key]
        public int RelationshipId { get; set; }

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
