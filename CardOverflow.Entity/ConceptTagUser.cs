using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class ConceptTagUser
    {
        public int ConceptId { get; set; }
        public int TagId { get; set; }
        public int UserId { get; set; }

        public virtual Concept Concept { get; set; }
        public virtual Tag Tag { get; set; }
        public virtual User User { get; set; }
    }
}
