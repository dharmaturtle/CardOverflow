using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FileEntity
    {
        public FileEntity()
        {
            File_ConceptInstances = new HashSet<File_ConceptInstanceEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string FileName { get; set; }
        [Required]
        public byte[] Data { get; set; }
        [Required]
        [MaxLength(32)]
        public byte[] Sha256 { get; set; }

        [InverseProperty("File")]
        public virtual ICollection<File_ConceptInstanceEntity> File_ConceptInstances { get; set; }
    }
}
