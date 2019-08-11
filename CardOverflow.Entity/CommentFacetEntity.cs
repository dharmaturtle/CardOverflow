using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentFacetEntity
    {
        public CommentFacetEntity()
        {
            Vote_CommentFacets = new HashSet<Vote_CommentFacetEntity>();
        }

        public int Id { get; set; }
        public int FacetId { get; set; }
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

        [ForeignKey("FacetId")]
        [InverseProperty("CommentFacets")]
        public virtual FacetEntity Facet { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentFacets")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentFacet")]
        public virtual ICollection<Vote_CommentFacetEntity> Vote_CommentFacets { get; set; }
    }
}
