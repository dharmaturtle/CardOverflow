using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptTemplateComment")]
    public partial class ConceptTemplateCommentEntity
    {
        public ConceptTemplateCommentEntity()
        {
            VoteConceptTemplateComments = new HashSet<VoteConceptTemplateCommentEntity>();
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
        [InverseProperty("ConceptTemplateComments")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("ConceptTemplateComments")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("ConceptTemplateComment")]
        public virtual ICollection<VoteConceptTemplateCommentEntity> VoteConceptTemplateComments { get; set; }
    }
}
