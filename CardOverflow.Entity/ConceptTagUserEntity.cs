using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Concept_Tag_User")]
    public partial class ConceptTagUserEntity
    {
        public int ConceptId { get; set; }
        public int TagId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("ConceptTagUsers")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("TagId")]
        [InverseProperty("ConceptTagUsers")]
        public virtual TagEntity Tag { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("ConceptTagUsers")]
        public virtual UserEntity User { get; set; }
    }
}
