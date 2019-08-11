using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_FacetEntity
    {
        public int FacetId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("FacetId")]
        [InverseProperty("Vote_Facets")]
        public virtual FacetEntity Facet { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_Facets")]
        public virtual UserEntity User { get; set; }
    }
}
