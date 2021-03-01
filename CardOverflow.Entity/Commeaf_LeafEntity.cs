using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("commeaf_2_revision")]
    public partial class Commeaf_RevisionEntity
    {
        [Key]
        public Guid RevisionId { get; set; }
        [Key]
        public Guid CommeafId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

        [ForeignKey("RevisionId")]
        [InverseProperty("Commeaf_Revisions")]
        public virtual RevisionEntity Revision { get; set; }
        [ForeignKey("CommeafId")]
        [InverseProperty("Commeaf_Revisions")]
        public virtual CommeafEntity Commeaf { get; set; }
    }
}
