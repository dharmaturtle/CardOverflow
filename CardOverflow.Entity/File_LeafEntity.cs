using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("file_2_revision")]
    public partial class File_RevisionEntity
    {
        [Key]
        public Guid RevisionId { get; set; }
        [Key]
        public Guid FileId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

        [ForeignKey("RevisionId")]
        [InverseProperty("File_Revisions")]
        public virtual RevisionEntity Revision { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("File_Revisions")]
        public virtual FileEntity File { get; set; }
    }
}
