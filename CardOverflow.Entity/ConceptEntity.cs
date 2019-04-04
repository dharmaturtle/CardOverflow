using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class ConceptEntity
    {
        public ConceptEntity()
        {
            Cards = new HashSet<CardEntity>();
            ConceptTagUsers = new HashSet<ConceptTagUserEntity>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public virtual ICollection<CardEntity> Cards { get; set; }
        public virtual ICollection<ConceptTagUserEntity> ConceptTagUsers { get; set; }
    }
}
