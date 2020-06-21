using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class BranchInstanceEntity
    {
        public BranchInstanceEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            CommunalFieldInstance_BranchInstances = new HashSet<CommunalFieldInstance_BranchInstanceEntity>();
            File_BranchInstances = new HashSet<File_BranchInstanceEntity>();
            Histories = new HashSet<HistoryEntity>();
            StackCopySources = new HashSet<StackEntity>();
            NotificationBranchInstances = new HashSet<NotificationEntity>();
        }

        [Key]
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public int StackId { get; set; }
        public int BranchId { get; set; }
        public bool IsDmca { get; set; }
        [Required]
        public string FieldValues { get; set; }
        public int CollateInstanceId { get; set; }
        public int Users { get; set; }
        [Required]
        [StringLength(200)]
        public string EditSummary {
            get => _EditSummary;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and EditSummary has a maximum length of 200. Attempted value: {value}");
                _EditSummary = value;
            }
        }
        private string _EditSummary;
        public long? AnkiNoteId { get; set; }
        [Required]
        [Column(TypeName = "bit(512)")]
        public BitArray Hash { get; set; }
        public string TsVectorHelper { get; set; }
        public NpgsqlTsVector TsVector { get; set; }
        public short MaxIndexInclusive { get; set; }

        [ForeignKey("StackId")]
        public virtual StackEntity Stack { get; set; }

        [ForeignKey("BranchId")]
        [InverseProperty("BranchInstances")]
        public virtual BranchEntity Branch { get; set; }
        [ForeignKey("CollateInstanceId")]
        [InverseProperty("BranchInstances")]
        public virtual CollateInstanceEntity CollateInstance { get; set; }
        [InverseProperty("BranchInstance")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("BranchInstance")]
        public virtual ICollection<CommunalFieldInstance_BranchInstanceEntity> CommunalFieldInstance_BranchInstances { get; set; }
        [InverseProperty("BranchInstance")]
        public virtual ICollection<File_BranchInstanceEntity> File_BranchInstances { get; set; }
        [InverseProperty("BranchInstance")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("BranchInstance")]
        public virtual ICollection<NotificationEntity> NotificationBranchInstances { get; set; }
        [InverseProperty("CopySource")]
        public virtual ICollection<StackEntity> StackCopySources { get; set; }
        public virtual ICollection<StackTagCountEntity> StackTagCounts { get; set; }
        public virtual ICollection<StackRelationshipCountEntity> StackRelationshipCounts { get; set; }
        public virtual ICollection<BranchInstanceTagCountEntity> BranchInstanceTagCounts { get; set; }
        public virtual ICollection<BranchInstanceRelationshipCountEntity> BranchInstanceRelationshipCounts { get; set; }
  }
}
