﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    [Table("file_2_leaf")]
    public partial class File_LeafEntity
    {
        [Key]
        public Guid LeafId { get; set; }
        [Key]
        public Guid FileId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [ForeignKey("LeafId")]
        [InverseProperty("File_Leafs")]
        public virtual LeafEntity Leaf { get; set; }
        [ForeignKey("FileId")]
        [InverseProperty("File_Leafs")]
        public virtual FileEntity File { get; set; }
    }
}
