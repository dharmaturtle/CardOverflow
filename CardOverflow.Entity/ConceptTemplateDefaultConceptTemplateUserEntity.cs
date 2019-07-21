using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptTemplateDefault_ConceptTemplate_User")]
    public partial class ConceptTemplateDefaultConceptTemplateUserEntity
    {
        public int ConceptTemplateId { get; set; }
        public int ConceptTemplateDefaultId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("ConceptTemplateDefaultConceptTemplateUsers")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("ConceptTemplateDefaultId")]
        [InverseProperty("ConceptTemplateDefaultConceptTemplateUsers")]
        public virtual ConceptTemplateDefaultEntity ConceptTemplateDefault { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("ConceptTemplateDefaultConceptTemplateUsers")]
        public virtual UserEntity User { get; set; }
    }
}
