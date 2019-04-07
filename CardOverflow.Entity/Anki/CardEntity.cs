using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity.Anki
{
[Table("cards")]
    public partial class CardEntity
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("nid")]
        public long Nid { get; set; }
        [Column("did")]
        public long Did { get; set; }
        [Column("ord")]
        public long Ord { get; set; }
        [Column("mod")]
        public long Mod { get; set; }
        [Column("usn")]
        public long Usn { get; set; }
        [Column("type")]
        public long Type { get; set; }
        [Column("queue")]
        public long Queue { get; set; }
        [Column("due")]
        public long Due { get; set; }
        [Column("ivl")]
        public long Ivl { get; set; }
        [Column("factor")]
        public long Factor { get; set; }
        [Column("reps")]
        public long Reps { get; set; }
        [Column("lapses")]
        public long Lapses { get; set; }
        [Column("left")]
        public long Left { get; set; }
        [Column("odue")]
        public long Odue { get; set; }
        [Column("odid")]
        public long Odid { get; set; }
        [Column("flags")]
        public long Flags { get; set; }
        [Required]
        [Column("data")]
        public string Data { get; set; }
    }
}
