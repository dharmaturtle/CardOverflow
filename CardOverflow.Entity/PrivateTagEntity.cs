using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PrivateTagEntity
    {
        public PrivateTagEntity()
        {
            PrivateTag_AcquiredCards = new HashSet<PrivateTag_AcquiredCardEntity>();
            PrivateTag_User_ConceptTemplateInstances = new HashSet<PrivateTag_User_ConceptTemplateInstanceEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        public string Name { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("PrivateTags")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("PrivateTag")]
        public virtual ICollection<PrivateTag_AcquiredCardEntity> PrivateTag_AcquiredCards { get; set; }
        [InverseProperty("DefaultPrivateTag")]
        public virtual ICollection<PrivateTag_User_ConceptTemplateInstanceEntity> PrivateTag_User_ConceptTemplateInstances { get; set; }
    }
}
