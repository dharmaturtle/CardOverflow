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
            PublicTag_User_ConceptTemplateInstances = new HashSet<PublicTag_User_ConceptTemplateInstanceEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        public string Name { get; set; }

        [InverseProperty("PublicTag")]
        public virtual ICollection<PublicTag_ConceptEntity> PublicTag_Concepts { get; set; }
        [InverseProperty("DefaultPublicTag")]
        public virtual ICollection<PublicTag_User_ConceptTemplateInstanceEntity> PublicTag_User_ConceptTemplateInstances { get; set; }
    }
}
