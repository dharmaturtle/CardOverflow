using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("User")]
    public partial class UserEntity
    {
        public UserEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            CardOptions = new HashSet<CardOptionEntity>();
            CommentConceptTemplates = new HashSet<CommentConceptTemplateEntity>();
            CommentConcepts = new HashSet<CommentConceptEntity>();
            ConceptTemplateDefaultConceptTemplateUsers = new HashSet<ConceptTemplateDefaultConceptTemplateUserEntity>();
            ConceptTemplates = new HashSet<ConceptTemplateEntity>();
            Concepts = new HashSet<ConceptEntity>();
            Decks = new HashSet<DeckEntity>();
            PrivateTags = new HashSet<PrivateTagEntity>();
            VoteCommentConceptTemplates = new HashSet<VoteCommentConceptTemplateEntity>();
            VoteCommentConcepts = new HashSet<VoteCommentConceptEntity>();
            VoteConceptTemplates = new HashSet<VoteConceptTemplateEntity>();
            VoteConcepts = new HashSet<VoteConceptEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(32)]
        public string DisplayName { get; set; }
        [Required]
        [StringLength(254)]
        public string Email { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<CardOptionEntity> CardOptions { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentConceptTemplateEntity> CommentConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentConceptEntity> CommentConcepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<ConceptTemplateDefaultConceptTemplateUserEntity> ConceptTemplateDefaultConceptTemplateUsers { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptTemplateEntity> ConceptTemplates { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<PrivateTagEntity> PrivateTags { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteCommentConceptTemplateEntity> VoteCommentConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteCommentConceptEntity> VoteCommentConcepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteConceptTemplateEntity> VoteConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteConceptEntity> VoteConcepts { get; set; }
    }
}
