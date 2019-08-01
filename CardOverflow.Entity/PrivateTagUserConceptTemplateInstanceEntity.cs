using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PrivateTag_User_ConceptTemplateInstance")]
    public partial class PrivateTagUserConceptTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int DefaultPrivateTagId { get; set; }

        [ForeignKey("DefaultPrivateTagId")]
        [InverseProperty("PrivateTagUserConceptTemplateInstances")]
        public virtual PrivateTagEntity DefaultPrivateTag { get; set; }
        [ForeignKey("UserId,ConceptTemplateInstanceId")]
        [InverseProperty("PrivateTagUserConceptTemplateInstances")]
        public virtual UserConceptTemplateInstanceEntity UserConceptTemplateInstance { get; set; }
    }
}
