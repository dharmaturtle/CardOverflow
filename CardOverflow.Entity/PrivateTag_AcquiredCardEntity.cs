using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PrivateTag_AcquiredCardEntity
    {
        public int PrivateTagId { get; set; }
        public int UserId { get; set; }
        public int ConceptInstanceId { get; set; }
        public int CardTemplateId { get; set; }

        [ForeignKey("UserId,ConceptInstanceId,CardTemplateId")]
        [InverseProperty("PrivateTag_AcquiredCards")]
        public virtual AcquiredCardEntity AcquiredCard { get; set; }
        [ForeignKey("PrivateTagId")]
        [InverseProperty("PrivateTag_AcquiredCards")]
        public virtual PrivateTagEntity PrivateTag { get; set; }
    }
}
