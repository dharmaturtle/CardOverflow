using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Concept_User")]
    public partial class ConceptUserEntity
    {
        public int UserId { get; set; }
        public int ConceptId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("ConceptUsers")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("ConceptUsers")]
        public virtual UserEntity User { get; set; }
    }
}
