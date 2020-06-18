using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class DeckFollowersEntity
    {
        [Key]
        public int DeckId { get; set; }
        [Key]
        public int FollowerId { get; set; }

        [ForeignKey("DeckId")]
        [InverseProperty("DeckFollowers")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("FollowerId")]
        [InverseProperty("DeckFollowers")]
        public virtual UserEntity Follower { get; set; }
    }
}
