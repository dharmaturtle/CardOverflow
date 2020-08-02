using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("relationship_2_card")]
    public partial class Relationship_CardEntity
    {
        [Key]
        public int RelationshipId { get; set; }
        [Key]
        public int UserId { get; set; }
        [Key]
        public int SourceStackId { get; set; }
        [Key]
        public int TargetStackId { get; set; }
        public int SourceCardId { get; set; }
        public int TargetCardId { get; set; }

        [ForeignKey("RelationshipId")]
        [InverseProperty("Relationship_Cards")]
        public virtual RelationshipEntity Relationship { get; set; }
        [ForeignKey("SourceCardId")]
        [InverseProperty("Relationship_CardSourceCards")]
        public virtual CardEntity SourceCard { get; set; }
        [ForeignKey("TargetCardId")]
        [InverseProperty("Relationship_CardTargetCards")]
        public virtual CardEntity TargetCard { get; set; }
    }
}
