using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Concept")]
    public partial class ConceptEntity
    {
        public ConceptEntity()
        {
            Cards = new HashSet<CardEntity>();
            FileConcepts = new HashSet<FileConceptEntity>();
            Children = new HashSet<ConceptEntity>();
            InversePrimaryChild_UseParentInstead = new HashSet<ConceptEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(128)]
        public string Title { get; set; }
        [Required]
        [StringLength(512)]
        public string Description { get; set; }
        public int ConceptTemplateId { get; set; }
        [Required]
        public string Fields { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Modified { get; set; }
        public int MaintainerId { get; set; }
        public bool IsPublic { get; set; }
        public int? ParentId { get; set; }
        public int? PrimaryChildId { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("Concepts")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("MaintainerId")]
        [InverseProperty("Concepts")]
        public virtual UserEntity Maintainer { get; set; }
        [ForeignKey("ParentId")]
        [InverseProperty("Children")]
        public virtual ConceptEntity Parent { get; set; }
        [ForeignKey("PrimaryChildId")]
        [InverseProperty("InversePrimaryChild_UseParentInstead")]
        public virtual ConceptEntity PrimaryChild { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<FileConceptEntity> FileConcepts { get; set; }
        [InverseProperty("Parent")]
        public virtual ICollection<ConceptEntity> Children { get; set; }
        [InverseProperty("PrimaryChild")]
        public virtual ICollection<ConceptEntity> InversePrimaryChild_UseParentInstead { get; set; }
    }
}
