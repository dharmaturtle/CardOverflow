using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentFacetEntity
    {
        public int CommentFacetId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("CommentFacetId")]
        [InverseProperty("Vote_CommentFacets")]
        public virtual CommentFacetEntity CommentFacet { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentFacets")]
        public virtual UserEntity User { get; set; }
    }
}
