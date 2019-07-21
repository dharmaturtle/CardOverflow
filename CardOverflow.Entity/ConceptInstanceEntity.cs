using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptInstance")]
    public partial class ConceptInstanceEntity
    {
        public ConceptInstanceEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            FileConceptInstances = new HashSet<FileConceptInstanceEntity>();
        }

        public int Id { get; set; }
        [Required]
        public string Fields { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime? Modified { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int ConceptId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("ConceptInstances")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("ConceptTemplateInstanceId")]
        [InverseProperty("ConceptInstances")]
        public virtual ConceptTemplateInstanceEntity ConceptTemplateInstance { get; set; }
        [InverseProperty("ConceptInstance")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("ConceptInstance")]
        public virtual ICollection<FileConceptInstanceEntity> FileConceptInstances { get; set; }
    }
}
