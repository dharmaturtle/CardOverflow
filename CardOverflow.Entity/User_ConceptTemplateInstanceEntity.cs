using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class User_ConceptTemplateInstanceEntity
    {
        public User_ConceptTemplateInstanceEntity()
        {
            PrivateTag_User_ConceptTemplateInstances = new HashSet<PrivateTag_User_ConceptTemplateInstanceEntity>();
            PublicTag_User_ConceptTemplateInstances = new HashSet<PublicTag_User_ConceptTemplateInstanceEntity>();
        }

        public int UserId { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int DefaultCardOptionId { get; set; }

        [ForeignKey("ConceptTemplateInstanceId")]
        [InverseProperty("User_ConceptTemplateInstances")]
        public virtual ConceptTemplateInstanceEntity ConceptTemplateInstance { get; set; }
        [ForeignKey("DefaultCardOptionId")]
        [InverseProperty("User_ConceptTemplateInstances")]
        public virtual CardOptionEntity DefaultCardOption { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_ConceptTemplateInstances")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("User_ConceptTemplateInstance")]
        public virtual ICollection<PrivateTag_User_ConceptTemplateInstanceEntity> PrivateTag_User_ConceptTemplateInstances { get; set; }
        [InverseProperty("User_ConceptTemplateInstance")]
        public virtual ICollection<PublicTag_User_ConceptTemplateInstanceEntity> PublicTag_User_ConceptTemplateInstances { get; set; }
    }
}
