using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommieldEntity
    {
        public CommieldEntity()
        {
            Commeafs = new HashSet<CommeafEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public int LatestInstanceId { get; set; }
        public bool IsListed { get; set; } = true;

        [ForeignKey("AuthorId")]
        [InverseProperty("Commields")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestInstanceId")]
        [InverseProperty("Commields")]
        public virtual CommeafEntity LatestInstance { get; set; }
        [InverseProperty("Commield")]
        public virtual ICollection<CommeafEntity> Commeafs { get; set; }
    }
}
