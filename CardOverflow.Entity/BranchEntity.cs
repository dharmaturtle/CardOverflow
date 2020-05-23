using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class BranchEntity
    {
        public BranchEntity()
        {
            AcquiredCardBranchNavigations = new HashSet<AcquiredCardEntity>();
            AcquiredCardBranches = new HashSet<AcquiredCardEntity>();
            BranchInstances = new HashSet<BranchInstanceEntity>();
        }

        [Key]
        public int Id { get; set; }
        [StringLength(64)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 64) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 64. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public int AuthorId { get; set; }
        public int StackId { get; set; }
        public int LatestInstanceId { get; set; }
        public int Users { get; set; }
        public bool IsListed { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Branches")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("StackId")]
        [InverseProperty("Branches")]
        public virtual StackEntity Stack { get; set; }
        [ForeignKey("LatestInstanceId")]
        public virtual BranchInstanceEntity LatestInstance { get; set; }
        public virtual ICollection<AcquiredCardEntity> AcquiredCardBranchNavigations { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCardBranches { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<BranchInstanceEntity> BranchInstances { get; set; }
    }
}
