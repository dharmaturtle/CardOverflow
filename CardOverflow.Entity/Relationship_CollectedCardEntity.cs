using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Relationship_CollectedCardEntity
    {
        [Key]
        public int RelationshipId { get; set; }
        [Key]
        public int UserId { get; set; }
        [Key]
        public int SourceStackId { get; set; }
        [Key]
        public int TargetStackId { get; set; }
        public int SourceCollectedCardId { get; set; }
        public int TargetCollectedCardId { get; set; }

        [ForeignKey("RelationshipId")]
        [InverseProperty("Relationship_CollectedCards")]
        public virtual RelationshipEntity Relationship { get; set; }
        [ForeignKey("SourceCollectedCardId")]
        [InverseProperty("Relationship_CollectedCardSourceCollectedCards")]
        public virtual CollectedCardEntity SourceCollectedCard { get; set; }
        [ForeignKey("TargetCollectedCardId")]
        [InverseProperty("Relationship_CollectedCardTargetCollectedCards")]
        public virtual CollectedCardEntity TargetCollectedCard { get; set; }
    }
}
