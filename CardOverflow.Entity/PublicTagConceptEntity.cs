using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PublicTag_Concept")]
    public partial class PublicTagConceptEntity
    {
        public int ConceptId { get; set; }
        public int PublicTagId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("PublicTagConcepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("PublicTagId")]
        [InverseProperty("PublicTagConcepts")]
        public virtual PublicTagEntity PublicTag { get; set; }
    }
}
