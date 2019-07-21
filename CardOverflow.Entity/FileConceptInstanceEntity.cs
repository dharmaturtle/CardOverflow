using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("File_ConceptInstance")]
    public partial class FileConceptInstanceEntity
    {
        public int ConceptInstanceId { get; set; }
        public int FileId { get; set; }

        [ForeignKey("ConceptInstanceId")]
        [InverseProperty("FileConceptInstances")]
        public virtual ConceptInstanceEntity ConceptInstance { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("FileConceptInstances")]
        public virtual FileEntity File { get; set; }
    }
}
