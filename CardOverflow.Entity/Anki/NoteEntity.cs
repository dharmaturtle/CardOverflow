using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity.Anki
{
[Table("notes")]
    public partial class NoteEntity
    {
        [Column("id")]
        public long Id { get; set; }
        [Required]
        [Column("guid")]
        public string Guid { get; set; }
        [Column("mid")]
        public long Mid { get; set; }
        [Column("mod")]
        public long Mod { get; set; }
        [Column("usn")]
        public long Usn { get; set; }
        [Required]
        [Column("tags")]
        public string Tags { get; set; }
        [Required]
        [Column("flds")]
        public string Flds { get; set; }
        [Column("sfld")]
        public long Sfld { get; set; }
        [Column("csum")]
        public long Csum { get; set; }
        [Column("flags")]
        public long Flags { get; set; }
        [Required]
        [Column("data")]
        public string Data { get; set; }
    }
}
