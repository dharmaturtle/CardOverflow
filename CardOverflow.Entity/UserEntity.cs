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
            CardTemplates = new HashSet<CardTemplateEntity>();
            Cards = new HashSet<CardEntity>();
            CommentCardTemplates = new HashSet<CommentCardTemplateEntity>();
            CommentCards = new HashSet<CommentCardEntity>();
            CommunalFields = new HashSet<CommunalFieldEntity>();
            Decks = new HashSet<DeckEntity>();
            Feedbacks = new HashSet<FeedbackEntity>();
            Relationships = new HashSet<RelationshipEntity>();
            User_CardTemplateInstances = new HashSet<User_CardTemplateInstanceEntity>();
            Vote_CommentCardTemplates = new HashSet<Vote_CommentCardTemplateEntity>();
            Vote_CommentCards = new HashSet<Vote_CommentCardEntity>();
            Vote_Feedbacks = new HashSet<Vote_FeedbackEntity>();
        }

        [Key]
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
        [InverseProperty("Author")]
        public virtual ICollection<CardTemplateEntity> CardTemplates { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentCardTemplateEntity> CommentCardTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<CommentCardEntity> CommentCards { get; set; }
        [InverseProperty("Author")]
        public virtual ICollection<CommunalFieldEntity> CommunalFields { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<FeedbackEntity> Feedbacks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<RelationshipEntity> Relationships { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<User_CardTemplateInstanceEntity> User_CardTemplateInstances { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentCardTemplateEntity> Vote_CommentCardTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_CommentCardEntity> Vote_CommentCards { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<Vote_FeedbackEntity> Vote_Feedbacks { get; set; }
    }
}

