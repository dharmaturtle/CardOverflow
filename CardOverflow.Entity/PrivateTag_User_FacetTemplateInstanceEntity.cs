using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PrivateTag_User_FacetTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int FacetTemplateInstanceId { get; set; }
        public int DefaultPrivateTagId { get; set; }

        [ForeignKey("DefaultPrivateTagId")]
        [InverseProperty("PrivateTag_User_FacetTemplateInstances")]
        public virtual PrivateTagEntity DefaultPrivateTag { get; set; }
        [ForeignKey("UserId,FacetTemplateInstanceId")]
        [InverseProperty("PrivateTag_User_FacetTemplateInstances")]
        public virtual User_FacetTemplateInstanceEntity User_FacetTemplateInstance { get; set; }
    }
}
