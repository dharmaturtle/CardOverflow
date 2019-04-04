using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class UserEntity
    {
        public UserEntity()
        {
            CardOptions = new HashSet<CardOptionEntity>();
            ConceptTagUsers = new HashSet<ConceptTagUserEntity>();
            Decks = new HashSet<DeckEntity>();
            Histories = new HashSet<HistoryEntity>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public virtual ICollection<CardOptionEntity> CardOptions { get; set; }
        public virtual ICollection<ConceptTagUserEntity> ConceptTagUsers { get; set; }
        public virtual ICollection<DeckEntity> Decks { get; set; }
        public virtual ICollection<HistoryEntity> Histories { get; set; }
    }
}
