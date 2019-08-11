using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FacetEntity
    {
        public FacetEntity()
        {
            CommentFacets = new HashSet<CommentFacetEntity>();
            FacetInstances = new HashSet<FacetInstanceEntity>();
            PublicTag_Facets = new HashSet<PublicTag_FacetEntity>();
            Vote_Facets = new HashSet<Vote_FacetEntity>();
        }

        public int Id { get; set; }
        public int MaintainerId { get; set; }
        [Required]
        [StringLength(100)]
        public string Description {
            get => _Description;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Description has a maximum length of 100. Attempted value: {value}");
                _Description = value;
            }
        }
        private string _Description;
        public int ConceptId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("Facets")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("MaintainerId")]
        [InverseProperty("Facets")]
        public virtual UserEntity Maintainer { get; set; }
        [InverseProperty("Facet")]
        public virtual ICollection<CommentFacetEntity> CommentFacets { get; set; }
        [InverseProperty("Facet")]
        public virtual ICollection<FacetInstanceEntity> FacetInstances { get; set; }
        [InverseProperty("Facet")]
        public virtual ICollection<PublicTag_FacetEntity> PublicTag_Facets { get; set; }
        [InverseProperty("Facet")]
        public virtual ICollection<Vote_FacetEntity> Vote_Facets { get; set; }
    }
}
