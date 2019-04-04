using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class TagEntity
    {
        public TagEntity()
        {
            ConceptTagUsers = new HashSet<ConceptTagUserEntity>();
            DeckTags = new HashSet<DeckTagEntity>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<ConceptTagUserEntity> ConceptTagUsers { get; set; }
        public virtual ICollection<DeckTagEntity> DeckTags { get; set; }
    }
}
