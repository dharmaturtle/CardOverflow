using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("relationship_2_card")]
    public partial class Relationship_CardEntity
    {
        [Key]
        public Guid RelationshipId { get; set; }
        [Key]
        public Guid UserId { get; set; }
        [Key]
        public Guid SourceConceptId { get; set; }
        [Key]
        public Guid TargetConceptId { get; set; }
        public Guid SourceCardId { get; set; }
        public Guid TargetCardId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

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
