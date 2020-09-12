using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;

namespace CardOverflow.Entity
{
    public partial class BranchEntity
    {
        public BranchEntity()
        {
            CardBranchNavigations = new HashSet<CardEntity>();
            CardBranches = new HashSet<CardEntity>();
            Leafs = new HashSet<LeafEntity>();
            NotificationBranches = new HashSet<NotificationEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [StringLength(64)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 64) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 64. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public Guid AuthorId { get; set; }
        public Guid StackId { get; set; }
        public Guid LatestId { get; set; }
        public int Users { get; set; }
        public bool IsListed { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("Branches")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestId")]
        public virtual LeafEntity Latest { get; set; }
        [ForeignKey("StackId")]
        [InverseProperty("Branches")]
        public virtual StackEntity Stack { get; set; }
        public virtual ICollection<CardEntity> CardBranchNavigations { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<CardEntity> CardBranches { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<LeafEntity> Leafs { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<NotificationEntity> NotificationBranches { get; set; }
    }
}
