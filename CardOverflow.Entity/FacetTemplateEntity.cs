using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FacetTemplateEntity
    {
        public FacetTemplateEntity()
        {
            CommentFacetTemplates = new HashSet<CommentFacetTemplateEntity>();
            FacetTemplateInstances = new HashSet<FacetTemplateInstanceEntity>();
            Vote_FacetTemplates = new HashSet<Vote_FacetTemplateEntity>();
        }

        public int Id { get; set; }
        public int MaintainerId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 100. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;

        [ForeignKey("MaintainerId")]
        [InverseProperty("FacetTemplates")]
        public virtual UserEntity Maintainer { get; set; }
        [InverseProperty("FacetTemplate")]
        public virtual ICollection<CommentFacetTemplateEntity> CommentFacetTemplates { get; set; }
        [InverseProperty("FacetTemplate")]
        public virtual ICollection<FacetTemplateInstanceEntity> FacetTemplateInstances { get; set; }
        [InverseProperty("FacetTemplate")]
        public virtual ICollection<Vote_FacetTemplateEntity> Vote_FacetTemplates { get; set; }
    }
}
