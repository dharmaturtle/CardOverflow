using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class AcquiredCardEntity
    {
        public AcquiredCardEntity()
        {
            Histories = new HashSet<HistoryEntity>();
            Relationship_AcquiredCardSourceAcquiredCards = new HashSet<Relationship_AcquiredCardEntity>();
            Relationship_AcquiredCardTargetAcquiredCards = new HashSet<Relationship_AcquiredCardEntity>();
            Tag_AcquiredCards = new HashSet<Tag_AcquiredCardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CardId { get; set; }
        public int BranchId { get; set; }
        public int BranchInstanceId { get; set; }
        public short CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        public DateTime Due { get; set; }
        public int CardSettingId { get; set; }
        public bool IsLapsed { get; set; }
        [Required]
        public string PersonalField { get; set; } = "";
        public string TsVectorHelper { get; set; }
        public NpgsqlTsVector TsVector { get; set; }

        [ForeignKey("BranchSourceIdOrCardId")]
        [InverseProperty("AllAcquiredCards")]
        public virtual CardEntity BranchSourceIdOrCard { get; set; }
        [ForeignKey("CardId")]
        [InverseProperty("AcquiredCards")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("CardInstanceId")]
        [InverseProperty("AcquiredCards")]
        public virtual CardInstanceEntity CardInstance { get; set; }
        [ForeignKey("CardSettingId")]
        [InverseProperty("AcquiredCards")]
        public virtual CardSettingEntity CardSetting { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("AcquiredCards")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("AcquiredCard")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("SourceAcquiredCard")]
        public virtual ICollection<Relationship_AcquiredCardEntity> Relationship_AcquiredCardSourceAcquiredCards { get; set; }
        [InverseProperty("TargetAcquiredCard")]
        public virtual ICollection<Relationship_AcquiredCardEntity> Relationship_AcquiredCardTargetAcquiredCards { get; set; }
        [InverseProperty("AcquiredCard")]
        public virtual ICollection<Tag_AcquiredCardEntity> Tag_AcquiredCards { get; set; }
    }
}
