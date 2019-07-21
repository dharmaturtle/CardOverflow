using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("PrivateTag_AcquiredCard")]
    public partial class PrivateTagAcquiredCardEntity
    {
        public int PrivateTagId { get; set; }
        public int UserId { get; set; }
        public int ConceptInstanceId { get; set; }
        public int CardTemplateId { get; set; }

        [ForeignKey("UserId,ConceptInstanceId,CardTemplateId")]
        [InverseProperty("PrivateTagAcquiredCards")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
        [ForeignKey("PrivateTagId")]
        [InverseProperty("PrivateTagAcquiredCards")]
        public virtual PrivateTagEntity PrivateTag { get; set; }
    }
}
