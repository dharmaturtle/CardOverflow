using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("CommentConceptTemplate")]
    public partial class CommentConceptTemplateEntity
    {
        public CommentConceptTemplateEntity()
        {
            VoteCommentConceptTemplates = new HashSet<VoteCommentConceptTemplateEntity>();
        }

        public int Id { get; set; }
        public int ConceptTemplateId { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(500)]
        public string Text { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("CommentConceptTemplates")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentConceptTemplates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentConceptTemplate")]
        public virtual ICollection<VoteCommentConceptTemplateEntity> VoteCommentConceptTemplates { get; set; }
    }
}
