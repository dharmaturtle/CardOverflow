using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class CollectedCardEntity
    {
        public CollectedCardEntity()
        {
            Histories = new HashSet<HistoryEntity>();
            Relationship_CollectedCardSourceCollectedCards = new HashSet<Relationship_CollectedCardEntity>();
            Relationship_CollectedCardTargetCollectedCards = new HashSet<Relationship_CollectedCardEntity>();
            Tag_CollectedCards = new HashSet<Tag_CollectedCardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int StackId { get; set; }
        public int BranchId { get; set; }
        public int BranchInstanceId { get; set; }
        public short Index { get; set; }
        public short CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        public DateTime Due { get; set; }
        public int CardSettingId { get; set; }
        public int DeckId { get; set; }
        public bool IsLapsed { get; set; }
        [Required]
        public string FrontPersonalField { get; set; } = "";
        [Required]
        public string BackPersonalField { get; set; } = "";
        public string TsVectorHelper { get; set; }
        public NpgsqlTsVector TsVector { get; set; }

        [ForeignKey("BranchId")]
        [InverseProperty("CollectedCardBranches")]
        public virtual BranchEntity Branch { get; set; }
        [ForeignKey("BranchInstanceId")]
        [InverseProperty("CollectedCards")]
        public virtual BranchInstanceEntity BranchInstance { get; set; }
        public virtual BranchEntity BranchNavigation { get; set; }
        [ForeignKey("CardSettingId")]
        [InverseProperty("CollectedCards")]
        public virtual CardSettingEntity CardSetting { get; set; }
        [ForeignKey("DeckId")]
        [InverseProperty("CollectedCards")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("StackId")]
        [InverseProperty("CollectedCards")]
        public virtual StackEntity Stack { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CollectedCards")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CollectedCard")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("SourceCollectedCard")]
        public virtual ICollection<Relationship_CollectedCardEntity> Relationship_CollectedCardSourceCollectedCards { get; set; }
        [InverseProperty("TargetCollectedCard")]
        public virtual ICollection<Relationship_CollectedCardEntity> Relationship_CollectedCardTargetCollectedCards { get; set; }
        [InverseProperty("CollectedCard")]
        public virtual ICollection<Tag_CollectedCardEntity> Tag_CollectedCards { get; set; }
    }
}
