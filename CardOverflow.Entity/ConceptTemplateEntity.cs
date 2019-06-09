using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptTemplate")]
    public partial class ConceptTemplateEntity
    {
        public ConceptTemplateEntity()
        {
            ConceptTemplateConceptTemplateDefaultUsers = new HashSet<ConceptTemplateConceptTemplateDefaultUserEntity>();
            Concepts = new HashSet<ConceptEntity>();
            Children = new HashSet<ConceptTemplateEntity>();
            InversePrimaryChild_UseParentInstead = new HashSet<ConceptTemplateEntity>();
        }

        public int Id { get; set; }
        public int MaintainerId { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [Required]
        [StringLength(1000)]
        public string Css { get; set; }
        [Required]
        [StringLength(300)]
        public string Fields { get; set; }
        [Required]
        [StringLength(1000)]
        public string CardTemplates { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Modified { get; set; }
        public bool IsCloze { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPre { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPost { get; set; }
        public int? ParentId { get; set; }
        public int? PrimaryChildId { get; set; }

        [ForeignKey("MaintainerId")]
        [InverseProperty("ConceptTemplates")]
        public virtual UserEntity Maintainer { get; set; }
        [ForeignKey("ParentId")]
        [InverseProperty("Children")]
        public virtual ConceptTemplateEntity Parent { get; set; }
        [ForeignKey("PrimaryChildId")]
        [InverseProperty("InversePrimaryChild_UseParentInstead")]
        public virtual ConceptTemplateEntity PrimaryChild { get; set; }
        [InverseProperty("ConceptTemplate")]
        public virtual ICollection<ConceptTemplateConceptTemplateDefaultUserEntity> ConceptTemplateConceptTemplateDefaultUsers { get; set; }
        [InverseProperty("ConceptTemplate")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
        [InverseProperty("Parent")]
        public virtual ICollection<ConceptTemplateEntity> Children { get; set; }
        [InverseProperty("PrimaryChild")]
        public virtual ICollection<ConceptTemplateEntity> InversePrimaryChild_UseParentInstead { get; set; }
    }
}
