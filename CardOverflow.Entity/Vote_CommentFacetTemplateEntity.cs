using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class Vote_CommentFacetTemplateEntity
    {
        public int CommentFacetTemplateId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("CommentFacetTemplateId")]
        [InverseProperty("Vote_CommentFacetTemplates")]
        public virtual CommentFacetTemplateEntity CommentFacetTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Vote_CommentFacetTemplates")]
        public virtual UserEntity User { get; set; }
    }
}
