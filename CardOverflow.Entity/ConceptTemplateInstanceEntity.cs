using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptTemplateInstance")]
    public partial class ConceptTemplateInstanceEntity
    {
        public ConceptTemplateInstanceEntity()
        {
            CardTemplates = new HashSet<CardTemplateEntity>();
            Fields = new HashSet<FieldEntity>();
        }

        public int Id { get; set; }
        public int ConceptTemplateId { get; set; }
        [Required]
        [StringLength(1000)]
        public string Css { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime? Modified { get; set; }
        public bool IsCloze { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPre { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPost { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("ConceptTemplateInstances")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [InverseProperty("ConceptTemplateInstance")]
        public virtual ICollection<CardTemplateEntity> CardTemplates { get; set; }
        [InverseProperty("ConceptTemplateInstance")]
        public virtual ICollection<FieldEntity> Fields { get; set; }
    }
}
