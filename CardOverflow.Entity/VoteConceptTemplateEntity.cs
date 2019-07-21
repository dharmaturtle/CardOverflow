using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Vote_ConceptTemplate")]
    public partial class VoteConceptTemplateEntity
    {
        public int ConceptTemplateId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("VoteConceptTemplates")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("VoteConceptTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
