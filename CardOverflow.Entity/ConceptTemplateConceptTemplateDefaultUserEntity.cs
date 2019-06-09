using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptTemplate_ConceptTemplateDefault_User")]
    public partial class ConceptTemplateConceptTemplateDefaultUserEntity
    {
        public int ConceptTemplateId { get; set; }
        public int ConceptTemplateDefaultId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("ConceptTemplateConceptTemplateDefaultUsers")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("ConceptTemplateDefaultId")]
        [InverseProperty("ConceptTemplateConceptTemplateDefaultUsers")]
        public virtual ConceptTemplateDefaultEntity ConceptTemplateDefault { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("ConceptTemplateConceptTemplateDefaultUsers")]
        public virtual UserEntity User { get; set; }
    }
}
