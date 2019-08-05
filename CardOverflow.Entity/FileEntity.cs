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
        public string FileName {
            get => _FileName;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FileName has a maximum length of 100. Attempted value: {value}");
                _FileName = value;
            }
        }
        private string _FileName;
        [Required]
        public byte[] Data { get; set; }
        [Required]
        [MaxLength(32)]
        public byte[] Sha256 { get; set; }

        [InverseProperty("File")]
        public virtual ICollection<File_ConceptInstanceEntity> File_ConceptInstances { get; set; }
    }
}
