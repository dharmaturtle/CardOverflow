using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class Concept
    {
        public Concept()
        {
            Cards = new HashSet<Card>();
            ConceptTagUsers = new HashSet<ConceptTagUser>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public virtual ICollection<Card> Cards { get; set; }
        public virtual ICollection<ConceptTagUser> ConceptTagUsers { get; set; }
    }
}
