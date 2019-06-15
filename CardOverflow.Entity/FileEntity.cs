using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("File")]
    public partial class FileEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string FileName { get; set; }
        [Required]
        public byte[] Data { get; set; }
        [Required]
        [MaxLength(32)]
        public byte[] Sha256 { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("Files")]
        public virtual UserEntity User { get; set; }
    }
}
