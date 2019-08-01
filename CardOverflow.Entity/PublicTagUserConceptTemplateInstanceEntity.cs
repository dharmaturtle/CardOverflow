using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PublicTag_User_ConceptTemplateInstance")]
    public partial class PublicTagUserConceptTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int DefaultPublicTagId { get; set; }

        [ForeignKey("DefaultPublicTagId")]
        [InverseProperty("PublicTagUserConceptTemplateInstances")]
        public virtual PublicTagEntity DefaultPublicTag { get; set; }
        [ForeignKey("UserId,ConceptTemplateInstanceId")]
        [InverseProperty("PublicTagUserConceptTemplateInstances")]
        public virtual UserConceptTemplateInstanceEntity UserConceptTemplateInstance { get; set; }
    }
}
