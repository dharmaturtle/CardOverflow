using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("User_ConceptTemplateInstance")]
    public partial class UserConceptTemplateInstanceEntity
    {
        public int UserId { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public int DefaultCardOptionId { get; set; }
        [Required]
        [StringLength(150)]
        public string DefaultPrivateTags { get; set; }
        [Required]
        [StringLength(150)]
        public string DefaultPublicTags { get; set; }

        [ForeignKey("ConceptTemplateInstanceId")]
        [InverseProperty("UserConceptTemplateInstances")]
        public virtual ConceptTemplateInstanceEntity ConceptTemplateInstance { get; set; }
        [ForeignKey("DefaultCardOptionId")]
        [InverseProperty("UserConceptTemplateInstances")]
        public virtual CardOptionEntity DefaultCardOption { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("UserConceptTemplateInstances")]
        public virtual UserEntity User { get; set; }
    }
}
