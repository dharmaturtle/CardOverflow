using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Relationship_CardInstanceEntity
    {
        [Key]
        public int UserId { get; set; }
        [Key]
        public int SourceInstanceId { get; set; }
        [Key]
        public int TargetInstanceId { get; set; }
        [Key]
        public int RelationshipId { get; set; }

        [ForeignKey("RelationshipId")]
        [InverseProperty("Relationship_CardInstances")]
        public virtual RelationshipEntity Relationship { get; set; }
        [ForeignKey("SourceInstanceId")]
        [InverseProperty("Relationship_CardInstanceSourceInstances")]
        public virtual CardInstanceEntity SourceInstance { get; set; }
        [ForeignKey("TargetInstanceId")]
        [InverseProperty("Relationship_CardInstanceTargetInstances")]
        public virtual CardInstanceEntity TargetInstance { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Relationship_CardInstances")]
        public virtual UserEntity User { get; set; }
    }
}
