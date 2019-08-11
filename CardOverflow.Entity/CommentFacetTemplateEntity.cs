using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentFacetTemplateEntity
    {
        public CommentFacetTemplateEntity()
        {
            Vote_CommentFacetTemplates = new HashSet<Vote_CommentFacetTemplateEntity>();
        }

        public int Id { get; set; }
        public int FacetTemplateId { get; set; }
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
        public bool IsDmca { get; set; }

        [ForeignKey("FacetTemplateId")]
        [InverseProperty("CommentFacetTemplates")]
        public virtual FacetTemplateEntity FacetTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentFacetTemplates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentFacetTemplate")]
        public virtual ICollection<Vote_CommentFacetTemplateEntity> Vote_CommentFacetTemplates { get; set; }
    }
}
