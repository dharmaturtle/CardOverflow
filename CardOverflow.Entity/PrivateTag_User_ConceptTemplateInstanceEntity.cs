using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PrivateTag_User_ConceptTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int DefaultPrivateTagId { get; set; }

        [ForeignKey("DefaultPrivateTagId")]
        [InverseProperty("PrivateTag_User_ConceptTemplateInstances")]
        public virtual PrivateTagEntity DefaultPrivateTag { get; set; }
        [ForeignKey("UserId,ConceptTemplateInstanceId")]
        [InverseProperty("PrivateTag_User_ConceptTemplateInstances")]
        public virtual User_ConceptTemplateInstanceEntity User_ConceptTemplateInstance { get; set; }
    }
}
