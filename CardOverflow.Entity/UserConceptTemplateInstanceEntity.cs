using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("User_ConceptTemplateInstance")]
    public partial class UserConceptTemplateInstanceEntity
    {
        public UserConceptTemplateInstanceEntity()
        {
            PrivateTagUserConceptTemplateInstances = new HashSet<PrivateTagUserConceptTemplateInstanceEntity>();
            PublicTagUserConceptTemplateInstances = new HashSet<PublicTagUserConceptTemplateInstanceEntity>();
        }

        public int UserId { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int DefaultCardOptionId { get; set; }

        [ForeignKey("ConceptTemplateInstanceId")]
        [InverseProperty("UserConceptTemplateInstances")]
        public virtual ConceptTemplateInstanceEntity ConceptTemplateInstance { get; set; }
        [ForeignKey("DefaultCardOptionId")]
        [InverseProperty("UserConceptTemplateInstances")]
        public virtual CardOptionEntity DefaultCardOption { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("UserConceptTemplateInstances")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("UserConceptTemplateInstance")]
        public virtual ICollection<PrivateTagUserConceptTemplateInstanceEntity> PrivateTagUserConceptTemplateInstances { get; set; }
        [InverseProperty("UserConceptTemplateInstance")]
        public virtual ICollection<PublicTagUserConceptTemplateInstanceEntity> PublicTagUserConceptTemplateInstances { get; set; }
    }
}
