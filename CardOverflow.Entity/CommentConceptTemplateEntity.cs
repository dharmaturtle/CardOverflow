using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentConceptTemplateEntity
    {
        public CommentConceptTemplateEntity()
        {
            Vote_CommentConceptTemplates = new HashSet<Vote_CommentConceptTemplateEntity>();
        }

        public int Id { get; set; }
        public int ConceptTemplateId { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(500)]
        public string Text {
            get => _Text;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Text has a maximum length of 500. Attempted value: {value}");
                _Text = value;
            }
        }
        private string _Text;
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("CommentConceptTemplates")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentConceptTemplates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentConceptTemplate")]
        public virtual ICollection<Vote_CommentConceptTemplateEntity> Vote_CommentConceptTemplates { get; set; }
    }
}
