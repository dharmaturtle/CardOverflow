using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_ConceptEntity
    {
        public int ConceptId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("Vote_Concepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_Concepts")]
        public virtual UserEntity User { get; set; }
    }
}
