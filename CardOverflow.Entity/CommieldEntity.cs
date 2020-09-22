using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class CommieldEntity
    {
        public CommieldEntity()
        {
            Commeafs = new HashSet<CommeafEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid AuthorId { get; set; }
        public Guid LatestId { get; set; }
        public bool IsListed { get; set; } = true;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Commields")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestId")]
        [InverseProperty("Commields")]
        public virtual CommeafEntity Latest { get; set; }
        [InverseProperty("Commield")]
        public virtual ICollection<CommeafEntity> Commeafs { get; set; }
    }
}
