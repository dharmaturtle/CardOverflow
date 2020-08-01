using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("file0branch_instance")]
    public partial class File_BranchInstanceEntity
    {
        [Key]
        public int BranchInstanceId { get; set; }
        [Key]
        public int FileId { get; set; }

        [ForeignKey("BranchInstanceId")]
        [InverseProperty("File_BranchInstances")]
        public virtual BranchInstanceEntity BranchInstance { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("File_BranchInstances")]
        public virtual FileEntity File { get; set; }
    }
}
