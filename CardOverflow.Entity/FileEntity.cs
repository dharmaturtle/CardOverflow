using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("File")]
    public partial class FileEntity
    {
        public FileEntity()
        {
            FileConceptInstances = new HashSet<FileConceptInstanceEntity>();
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
        public virtual ICollection<FileConceptInstanceEntity> FileConceptInstances { get; set; }
    }
}
