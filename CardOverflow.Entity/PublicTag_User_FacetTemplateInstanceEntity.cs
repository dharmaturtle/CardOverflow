using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PublicTag_User_FacetTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int FacetTemplateInstanceId { get; set; }
        public int DefaultPublicTagId { get; set; }

        [ForeignKey("DefaultPublicTagId")]
        [InverseProperty("PublicTag_User_FacetTemplateInstances")]
        public virtual PublicTagEntity DefaultPublicTag { get; set; }
        [ForeignKey("UserId,FacetTemplateInstanceId")]
        [InverseProperty("PublicTag_User_FacetTemplateInstances")]
        public virtual User_FacetTemplateInstanceEntity User_FacetTemplateInstance { get; set; }
    }
}
