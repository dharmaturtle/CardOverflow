﻿using System;
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
            CardOptions = new HashSet<CardOptionEntity>();
            ConceptTemplates = new HashSet<ConceptTemplateEntity>();
            ConceptUsers = new HashSet<ConceptUserEntity>();
            Concepts = new HashSet<ConceptEntity>();
            Decks = new HashSet<DeckEntity>();
            Files = new HashSet<FileEntity>();
            Histories = new HashSet<HistoryEntity>();
            PrivateTags = new HashSet<PrivateTagEntity>();
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
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptTemplateEntity> ConceptTemplates { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<ConceptUserEntity> ConceptUsers { get; set; }
        [InverseProperty("Maintainer")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<DeckEntity> Decks { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<FileEntity> Files { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<PrivateTagEntity> PrivateTags { get; set; }
    }
}
