using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class ConceptTagUserEntity
    {
        public int ConceptId { get; set; }
        public int TagId { get; set; }
        public int UserId { get; set; }

        public virtual ConceptEntity Concept { get; set; }
        public virtual TagEntity Tag { get; set; }
        public virtual UserEntity User { get; set; }
    }
}
