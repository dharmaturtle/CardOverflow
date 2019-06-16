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
            FileConcepts = new HashSet<FileConceptEntity>();
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
        public virtual ICollection<FileConceptEntity> FileConcepts { get; set; }
    }
}
