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
            CollectedCardBranchNavigations = new HashSet<CollectedCardEntity>();
            CollectedCardBranches = new HashSet<CollectedCardEntity>();
            Leafs = new HashSet<LeafEntity>();
            NotificationBranches = new HashSet<NotificationEntity>();
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
        [ForeignKey("LatestInstanceId")]
        public virtual LeafEntity LatestInstance { get; set; }
        [ForeignKey("StackId")]
        [InverseProperty("Branches")]
        public virtual StackEntity Stack { get; set; }
        public virtual ICollection<CollectedCardEntity> CollectedCardBranchNavigations { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<CollectedCardEntity> CollectedCardBranches { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<LeafEntity> Leafs { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<NotificationEntity> NotificationBranches { get; set; }
    }
}
