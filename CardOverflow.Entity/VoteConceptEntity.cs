using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Vote_Concept")]
    public partial class VoteConceptEntity
    {
        public int ConceptId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("VoteConcepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("VoteConcepts")]
        public virtual UserEntity User { get; set; }
    }
}
