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
            ConceptComments = new HashSet<ConceptCommentEntity>();
            ConceptTemplateComments = new HashSet<ConceptTemplateCommentEntity>();
            ConceptTemplateConceptTemplateDefaultUsers = new HashSet<ConceptTemplateConceptTemplateDefaultUserEntity>();
            ConceptTemplates = new HashSet<ConceptTemplateEntity>();
            Concepts = new HashSet<ConceptEntity>();
            Decks = new HashSet<DeckEntity>();
            PrivateTags = new HashSet<PrivateTagEntity>();
            VoteConceptComments = new HashSet<VoteConceptCommentEntity>();
            VoteConceptTemplateComments = new HashSet<VoteConceptTemplateCommentEntity>();
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
        public virtual ICollection<ConceptCommentEntity> ConceptComments { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<ConceptTemplateCommentEntity> ConceptTemplateComments { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<ConceptTemplateConceptTemplateDefaultUserEntity> ConceptTemplateConceptTemplateDefaultUsers { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptTemplateEntity> ConceptTemplates { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<PrivateTagEntity> PrivateTags { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteConceptCommentEntity> VoteConceptComments { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteConceptTemplateCommentEntity> VoteConceptTemplateComments { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteConceptTemplateEntity> VoteConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<VoteConceptEntity> VoteConcepts { get; set; }
    }
}
