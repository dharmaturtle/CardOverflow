using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_ConceptTemplateEntity
    {
        public int ConceptTemplateId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("Vote_ConceptTemplates")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_ConceptTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
