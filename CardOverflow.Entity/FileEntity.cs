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
            File_FacetInstances = new HashSet<File_FacetInstanceEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string FileName {
            get => _FileName;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FileName has a maximum length of 200. Attempted value: {value}");
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
        public virtual ICollection<File_FacetInstanceEntity> File_FacetInstances { get; set; }
    }
}
