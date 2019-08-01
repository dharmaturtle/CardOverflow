using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class File_ConceptInstanceEntity
    {
        public int ConceptInstanceId { get; set; }
        public int FileId { get; set; }

        [ForeignKey("ConceptInstanceId")]
        [InverseProperty("File_ConceptInstances")]
        public virtual ConceptInstanceEntity ConceptInstance { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("File_ConceptInstances")]
        public virtual FileEntity File { get; set; }
    }
}
