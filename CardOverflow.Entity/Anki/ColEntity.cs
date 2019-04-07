using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity.Anki
{
[Table("col")]
    public partial class ColEntity
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("crt")]
        public long Crt { get; set; }
        [Column("mod")]
        public long Mod { get; set; }
        [Column("scm")]
        public long Scm { get; set; }
        [Column("ver")]
        public long Ver { get; set; }
        [Column("dty")]
        public long Dty { get; set; }
        [Column("usn")]
        public long Usn { get; set; }
        [Column("ls")]
        public long Ls { get; set; }
        [Required]
        [Column("conf")]
        public string Conf { get; set; }
        [Required]
        [Column("models")]
        public string Models { get; set; }
        [Required]
        [Column("decks")]
        public string Decks { get; set; }
        [Required]
        [Column("dconf")]
        public string Dconf { get; set; }
        [Required]
        [Column("tags")]
        public string Tags { get; set; }
    }
}
