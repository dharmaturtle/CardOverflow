using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PublicTag_FacetEntity
    {
        public int FacetId { get; set; }
        public int PublicTagId { get; set; }

        [ForeignKey("FacetId")]
        [InverseProperty("PublicTag_Facets")]
        public virtual FacetEntity Facet { get; set; }
        [ForeignKey("PublicTagId")]
        [InverseProperty("PublicTag_Facets")]
        public virtual PublicTagEntity PublicTag { get; set; }
    }
}
