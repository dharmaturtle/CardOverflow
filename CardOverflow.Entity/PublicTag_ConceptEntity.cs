using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PublicTag_ConceptEntity
    {
        public int ConceptId { get; set; }
        public int PublicTagId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("PublicTag_Concepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("PublicTagId")]
        [InverseProperty("PublicTag_Concepts")]
        public virtual PublicTagEntity PublicTag { get; set; }
    }
}
