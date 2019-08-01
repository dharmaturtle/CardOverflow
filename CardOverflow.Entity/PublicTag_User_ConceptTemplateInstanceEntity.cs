using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PublicTag_User_ConceptTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int DefaultPublicTagId { get; set; }

        [ForeignKey("DefaultPublicTagId")]
        [InverseProperty("PublicTag_User_ConceptTemplateInstances")]
        public virtual PublicTagEntity DefaultPublicTag { get; set; }
        [ForeignKey("UserId,ConceptTemplateInstanceId")]
        [InverseProperty("PublicTag_User_ConceptTemplateInstances")]
        public virtual User_ConceptTemplateInstanceEntity User_ConceptTemplateInstance { get; set; }
    }
}
