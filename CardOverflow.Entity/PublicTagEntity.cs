using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PublicTagEntity
    {
        public PublicTagEntity()
        {
            PublicTag_Concepts = new HashSet<PublicTag_ConceptEntity>();
            PublicTag_User_FacetTemplateInstances = new HashSet<PublicTag_User_FacetTemplateInstanceEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 250) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 250. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;

        [InverseProperty("PublicTag")]
        public virtual ICollection<PublicTag_ConceptEntity> PublicTag_Concepts { get; set; }
        [InverseProperty("DefaultPublicTag")]
        public virtual ICollection<PublicTag_User_FacetTemplateInstanceEntity> PublicTag_User_FacetTemplateInstances { get; set; }
    }
}
