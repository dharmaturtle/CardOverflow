using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class Tag
    {
        public Tag()
        {
            ConceptTagUsers = new HashSet<ConceptTagUser>();
            DeckTags = new HashSet<DeckTag>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<ConceptTagUser> ConceptTagUsers { get; set; }
        public virtual ICollection<DeckTag> DeckTags { get; set; }
    }
}
