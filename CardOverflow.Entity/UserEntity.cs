using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CardOverflow.Entity
{
    public partial class UserEntity : IdentityUser<int>
    {
        public UserEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            CardOptions = new HashSet<CardOptionEntity>();
            CommentConceptTemplates = new HashSet<CommentConceptTemplateEntity>();
            CommentConcepts = new HashSet<CommentConceptEntity>();
            ConceptTemplates = new HashSet<ConceptTemplateEntity>();
            Concepts = new HashSet<ConceptEntity>();
            Decks = new HashSet<DeckEntity>();
            PrivateTags = new HashSet<PrivateTagEntity>();
            User_ConceptTemplateInstances = new HashSet<User_ConceptTemplateInstanceEntity>();
            Vote_CommentConceptTemplates = new HashSet<Vote_CommentConceptTemplateEntity>();
            Vote_CommentConcepts = new HashSet<Vote_CommentConceptEntity>();
            Vote_ConceptTemplates = new HashSet<Vote_ConceptTemplateEntity>();
            Vote_Concepts = new HashSet<Vote_ConceptEntity>();
        }

        //[Required] // medTODO
        [StringLength(32)]
        public string DisplayName { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<CardOptionEntity> CardOptions { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentConceptTemplateEntity> CommentConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentConceptEntity> CommentConcepts { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptTemplateEntity> ConceptTemplates { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<PrivateTagEntity> PrivateTags { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<User_ConceptTemplateInstanceEntity> User_ConceptTemplateInstances { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentConceptTemplateEntity> Vote_CommentConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentConceptEntity> Vote_CommentConcepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_ConceptTemplateEntity> Vote_ConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_ConceptEntity> Vote_Concepts { get; set; }
    }
}
