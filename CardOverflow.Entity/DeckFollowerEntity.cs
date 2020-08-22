using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class DeckFollowerEntity
    {
        [Key]
        public Guid DeckId { get; set; }
        [Key]
        public Guid FollowerId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [ForeignKey("DeckId")]
        [InverseProperty("DeckFollowers")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("FollowerId")]
        [InverseProperty("DeckFollowers")]
        public virtual UserEntity Follower { get; set; }
    }
}
