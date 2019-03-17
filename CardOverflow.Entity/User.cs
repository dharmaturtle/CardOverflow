using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class User
    {
        public User()
        {
            Decks = new HashSet<Deck>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public virtual ICollection<Deck> Decks { get; set; }
    }
}
