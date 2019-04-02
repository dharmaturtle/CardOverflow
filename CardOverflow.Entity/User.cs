using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class User
    {
        public User()
        {
            CardOptions = new HashSet<CardOption>();
            ConceptTagUsers = new HashSet<ConceptTagUser>();
            Decks = new HashSet<Deck>();
            Histories = new HashSet<History>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public virtual ICollection<CardOption> CardOptions { get; set; }
        public virtual ICollection<ConceptTagUser> ConceptTagUsers { get; set; }
        public virtual ICollection<Deck> Decks { get; set; }
        public virtual ICollection<History> Histories { get; set; }
    }
}
