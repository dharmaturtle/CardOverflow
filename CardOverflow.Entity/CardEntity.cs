using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardEntity
    {
        public CardEntity()
        {
            CardInstances = new HashSet<CardInstanceEntity>();
            CommentCards = new HashSet<CommentCardEntity>();
            RelationshipSources = new HashSet<RelationshipEntity>();
            RelationshipTargets = new HashSet<RelationshipEntity>();
        }

        public int Id { get; set; }
        public int AuthorId { get; set; }
        [Required]
        [StringLength(100)]
        public string Description {
            get => _Description;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Description has a maximum length of 100. Attempted value: {value}");
                _Description = value;
            }
        }
        private string _Description;

        [ForeignKey("AuthorId")]
        [InverseProperty("Cards")]
        public virtual UserEntity Author { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CardInstanceEntity> CardInstances { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
        [InverseProperty("Source")]
        public virtual ICollection<RelationshipEntity> RelationshipSources { get; set; }
        [InverseProperty("Target")]
        public virtual ICollection<RelationshipEntity> RelationshipTargets { get; set; }
    }
}
