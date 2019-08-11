using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class User_FacetTemplateInstanceEntity
    {
        public User_FacetTemplateInstanceEntity()
        {
            PrivateTag_User_FacetTemplateInstances = new HashSet<PrivateTag_User_FacetTemplateInstanceEntity>();
            PublicTag_User_FacetTemplateInstances = new HashSet<PublicTag_User_FacetTemplateInstanceEntity>();
        }

        public int UserId { get; set; }
        public int FacetTemplateInstanceId { get; set; }
        public int DefaultCardOptionId { get; set; }

        [ForeignKey("DefaultCardOptionId")]
        [InverseProperty("User_FacetTemplateInstances")]
        public virtual CardOptionEntity DefaultCardOption { get; set; }
        [ForeignKey("FacetTemplateInstanceId")]
        [InverseProperty("User_FacetTemplateInstances")]
        public virtual FacetTemplateInstanceEntity FacetTemplateInstance { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_FacetTemplateInstances")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("User_FacetTemplateInstance")]
        public virtual ICollection<PrivateTag_User_FacetTemplateInstanceEntity> PrivateTag_User_FacetTemplateInstances { get; set; }
        [InverseProperty("User_FacetTemplateInstance")]
        public virtual ICollection<PublicTag_User_FacetTemplateInstanceEntity> PublicTag_User_FacetTemplateInstances { get; set; }
    }
}
