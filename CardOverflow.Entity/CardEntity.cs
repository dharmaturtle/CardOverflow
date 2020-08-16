using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class CardEntity
    {
        public CardEntity()
        {
            Histories = new HashSet<HistoryEntity>();
            Relationship_CardSourceCards = new HashSet<Relationship_CardEntity>();
            Relationship_CardTargetCards = new HashSet<Relationship_CardEntity>();
            Tag_Cards = new HashSet<Tag_CardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int StackId { get; set; }
        public int BranchId { get; set; }
        public int LeafId { get; set; }
        public short Index { get; set; }
        public short CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        public DateTime Due { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public int CardSettingId { get; set; }
        public int DeckId { get; set; }
        public bool IsLapsed { get; set; }
        [Required]
        [StringLength(5000)]
        public string FrontPersonalField
        {
            get => _FrontPersonalField;
            set
            {
                if (value.Length > 5000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FrontPersonalField has a maximum length of 5000. Attempted value: {value}");
                _FrontPersonalField = value;
            }
        }
        private string _FrontPersonalField = "";
        [Required]
        [StringLength(5000)]
        public string BackPersonalField
        {
            get => _BackPersonalField;
            set
            {
                if (value.Length > 5000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and BackPersonalField has a maximum length of 5000. Attempted value: {value}");
                _BackPersonalField = value;
            }
        }
        private string _BackPersonalField = "";
        public string TsvHelper { get; set; }
        public NpgsqlTsVector Tsv { get; set; }

        [ForeignKey("BranchId")]
        [InverseProperty("CardBranches")]
        public virtual BranchEntity Branch { get; set; }
        [ForeignKey("LeafId")]
        [InverseProperty("Cards")]
        public virtual LeafEntity Leaf { get; set; }
        public virtual BranchEntity BranchNavigation { get; set; }
        [ForeignKey("CardSettingId")]
        [InverseProperty("Cards")]
        public virtual CardSettingEntity CardSetting { get; set; }
        [ForeignKey("DeckId")]
        [InverseProperty("Cards")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("StackId")]
        [InverseProperty("Cards")]
        public virtual StackEntity Stack { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Cards")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("SourceCard")]
        public virtual ICollection<Relationship_CardEntity> Relationship_CardSourceCards { get; set; }
        [InverseProperty("TargetCard")]
        public virtual ICollection<Relationship_CardEntity> Relationship_CardTargetCards { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<Tag_CardEntity> Tag_Cards { get; set; }
    }
}
