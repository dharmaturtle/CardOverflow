using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptComment")]
    public partial class ConceptCommentEntity
    {
        public ConceptCommentEntity()
        {
            VoteConceptComments = new HashSet<VoteConceptCommentEntity>();
        }

        public int Id { get; set; }
        public int ConceptId { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(500)]
        public string Text { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("ConceptComments")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("ConceptComments")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("ConceptComment")]
        public virtual ICollection<VoteConceptCommentEntity> VoteConceptComments { get; set; }
    }
}
