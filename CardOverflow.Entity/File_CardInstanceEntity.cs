using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class File_CardInstanceEntity
    {
        public int CardInstanceId { get; set; }
        public int FileId { get; set; }

        [ForeignKey("CardInstanceId")]
        [InverseProperty("File_CardInstances")]
        public virtual CardInstanceEntity CardInstance { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("File_CardInstances")]
        public virtual FileEntity File { get; set; }
    }
}
