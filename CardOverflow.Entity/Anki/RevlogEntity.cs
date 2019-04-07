using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity.Anki
{
[Table("revlog")]
    public partial class RevlogEntity
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("cid")]
        public long Cid { get; set; }
        [Column("usn")]
        public long Usn { get; set; }
        [Column("ease")]
        public long Ease { get; set; }
        [Column("ivl")]
        public long Ivl { get; set; }
        [Column("lastIvl")]
        public long LastIvl { get; set; }
        [Column("factor")]
        public long Factor { get; set; }
        [Column("time")]
        public long Time { get; set; }
        [Column("type")]
        public long Type { get; set; }
    }
}
