using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PrivateTag_Concept")]
    public partial class PrivateTagConceptEntity
    {
        public int ConceptId { get; set; }
        public int PrivateTagId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("PrivateTagConcepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("PrivateTagId")]
        [InverseProperty("PrivateTagConcepts")]
        public virtual PrivateTagEntity PrivateTag { get; set; }
    }
}
