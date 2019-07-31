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
            CommentConceptTemplates = new HashSet<CommentConceptTemplateEntity>();
            ConceptTemplateInstances = new HashSet<ConceptTemplateInstanceEntity>();
            VoteConceptTemplates = new HashSet<VoteConceptTemplateEntity>();
        }

        public int Id { get; set; }
        public int MaintainerId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [ForeignKey("MaintainerId")]
        [InverseProperty("ConceptTemplates")]
        public virtual UserEntity Maintainer { get; set; }
        [InverseProperty("ConceptTemplate")]
        public virtual ICollection<CommentConceptTemplateEntity> CommentConceptTemplates { get; set; }
        [InverseProperty("ConceptTemplate")]
        public virtual ICollection<ConceptTemplateInstanceEntity> ConceptTemplateInstances { get; set; }
        [InverseProperty("ConceptTemplate")]
        public virtual ICollection<VoteConceptTemplateEntity> VoteConceptTemplates { get; set; }
    }
}
