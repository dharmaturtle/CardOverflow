using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class LeafEntity
    {
        public LeafEntity()
        {
            Cards = new HashSet<CardEntity>();
            Commeaf_Leafs = new HashSet<Commeaf_LeafEntity>();
            File_Leafs = new HashSet<File_LeafEntity>();
            Histories = new HashSet<HistoryEntity>();
            StackCopySources = new HashSet<StackEntity>();
            NotificationLeafs = new HashSet<NotificationEntity>();
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
        public int GrompleafId { get; set; }
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
        [InverseProperty("Leafs")]
        public virtual BranchEntity Branch { get; set; }
        [ForeignKey("GrompleafId")]
        [InverseProperty("Leafs")]
        public virtual GrompleafEntity Grompleaf { get; set; }
        [InverseProperty("Leaf")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Leaf")]
        public virtual ICollection<Commeaf_LeafEntity> Commeaf_Leafs { get; set; }
        [InverseProperty("Leaf")]
        public virtual ICollection<File_LeafEntity> File_Leafs { get; set; }
        [InverseProperty("Leaf")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("Leaf")]
        public virtual ICollection<NotificationEntity> NotificationLeafs { get; set; }
        [InverseProperty("CopySource")]
        public virtual ICollection<StackEntity> StackCopySources { get; set; }
        public virtual ICollection<StackTagCountEntity> StackTagCounts { get; set; }
        public virtual ICollection<StackRelationshipCountEntity> StackRelationshipCounts { get; set; }
        public virtual ICollection<LeafTagCountEntity> LeafTagCounts { get; set; }
        public virtual ICollection<LeafRelationshipCountEntity> LeafRelationshipCounts { get; set; }
  }
}
