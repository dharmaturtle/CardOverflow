using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("File_Concept")]
    public partial class FileConceptEntity
    {
        public int ConceptId { get; set; }
        public int FileId { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("FileConcepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("FileConcepts")]
        public virtual FileEntity File { get; set; }
    }
}
