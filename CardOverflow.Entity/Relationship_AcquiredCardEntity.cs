using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Relationship_AcquiredCardEntity
    {
        [Key]
        public int AcquiredCardId { get; set; }
        [Key]
        public int RelationshipId { get; set; }

        [ForeignKey("AcquiredCardId")]
        [InverseProperty("Relationship_AcquiredCards")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
        [ForeignKey("RelationshipId")]
        [InverseProperty("Relationship_AcquiredCards")]
        public virtual RelationshipEntity Relationship { get; set; }
    }
}
