using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;
using NpgsqlTypes;
using NUlid;
using NodaTime;

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
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        public Guid StackId { get; set; }
        public Guid BranchId { get; set; }
        public bool IsDmca { get; set; }
        [Required]
        [StringLength(10000)]
        public string FieldValues
        {
            get => _FieldValues;
            set
            {
                if (value.Length > 10000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FieldValues has a maximum length of 10000. Attempted value: {value}");
                _FieldValues = value;
            }
        }
        private string _FieldValues;
        public Guid GrompleafId { get; set; }
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
        public string TsvHelper { get; set; }
        public NpgsqlTsVector Tsv { get; set; }
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
