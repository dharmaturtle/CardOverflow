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
            CommentFacetTemplates = new HashSet<CommentFacetTemplateEntity>();
            CommentFacets = new HashSet<CommentFacetEntity>();
            Concepts = new HashSet<ConceptEntity>();
            Decks = new HashSet<DeckEntity>();
            FacetTemplates = new HashSet<FacetTemplateEntity>();
            Facets = new HashSet<FacetEntity>();
            PrivateTags = new HashSet<PrivateTagEntity>();
            User_FacetTemplateInstances = new HashSet<User_FacetTemplateInstanceEntity>();
            Vote_CommentFacetTemplates = new HashSet<Vote_CommentFacetTemplateEntity>();
            Vote_CommentFacets = new HashSet<Vote_CommentFacetEntity>();
            Vote_FacetTemplates = new HashSet<Vote_FacetTemplateEntity>();
            Vote_Facets = new HashSet<Vote_FacetEntity>();
        }

        //[Required] // medTODO make this not nullable
        [StringLength(32)]
        public string DisplayName {
            get => _DisplayName;
            set {
                if (value.Length > 32) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and DisplayName has a maximum length of 32. Attempted value: {value}");
                _DisplayName = value;
            }
        }
        private string _DisplayName;

        [InverseProperty("User")]
        public virtual ICollection<CardOptionEntity> CardOptions { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentFacetTemplateEntity> CommentFacetTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentFacetEntity> CommentFacets { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<FacetTemplateEntity> FacetTemplates { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<FacetEntity> Facets { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<PrivateTagEntity> PrivateTags { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<User_FacetTemplateInstanceEntity> User_FacetTemplateInstances { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentFacetTemplateEntity> Vote_CommentFacetTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentFacetEntity> Vote_CommentFacets { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_FacetTemplateEntity> Vote_FacetTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_FacetEntity> Vote_Facets { get; set; }
    }
}

