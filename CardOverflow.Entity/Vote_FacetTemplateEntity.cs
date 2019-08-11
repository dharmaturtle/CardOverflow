using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_FacetTemplateEntity
    {
        public int FacetTemplateId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("FacetTemplateId")]
        [InverseProperty("Vote_FacetTemplates")]
        public virtual FacetTemplateEntity FacetTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_FacetTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
