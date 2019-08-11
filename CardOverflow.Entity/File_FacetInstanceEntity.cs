using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class File_FacetInstanceEntity
    {
        public int FacetInstanceId { get; set; }
        public int FileId { get; set; }

        [ForeignKey("FacetInstanceId")]
        [InverseProperty("File_FacetInstances")]
        public virtual FacetInstanceEntity FacetInstance { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("File_FacetInstances")]
        public virtual FileEntity File { get; set; }
    }
}
