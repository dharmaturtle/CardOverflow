using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class DeckEntity
    {
        public DeckEntity()
        {
            DeckCards = new HashSet<DeckCardEntity>();
            DeckTags = new HashSet<DeckTagEntity>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }

        public virtual UserEntity User { get; set; }
        public virtual ICollection<DeckCardEntity> DeckCards { get; set; }
        public virtual ICollection<DeckTagEntity> DeckTags { get; set; }
    }
}
