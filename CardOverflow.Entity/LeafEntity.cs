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
    public partial class RevisionEntity
    {
        public RevisionEntity()
        {
            Cards = new HashSet<CardEntity>();
            Commeaf_Revisions = new HashSet<Commeaf_RevisionEntity>();
            File_Revisions = new HashSet<File_RevisionEntity>();
            Histories = new HashSet<HistoryEntity>();
            ConceptCopySources = new HashSet<ConceptEntity>();
            NotificationRevisions = new HashSet<NotificationEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        public Guid ConceptId { get; set; }
        public Guid ExampleId { get; set; }
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
        public Guid TemplateRevisionId { get; set; }
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
        [Required]
        [StringLength(300)]
        public string[] Tags { get; set; } = new string[0];
        [Required]
        public int[] TagsCount { get; set; } = new int[0];

        [ForeignKey("ConceptId")]
        public virtual ConceptEntity Concept { get; set; }

        [ForeignKey("ExampleId")]
        [InverseProperty("Revisions")]
        public virtual ExampleEntity Example { get; set; }
        [ForeignKey("TemplateRevisionId")]
        [InverseProperty("Revisions")]
        public virtual TemplateRevisionEntity TemplateRevision { get; set; }
        [InverseProperty("Revision")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Revision")]
        public virtual ICollection<Commeaf_RevisionEntity> Commeaf_Revisions { get; set; }
        [InverseProperty("Revision")]
        public virtual ICollection<File_RevisionEntity> File_Revisions { get; set; }
        [InverseProperty("Revision")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
        [InverseProperty("Revision")]
        public virtual ICollection<NotificationEntity> NotificationRevisions { get; set; }
        [InverseProperty("CopySource")]
        public virtual ICollection<ConceptEntity> ConceptCopySources { get; set; }
        public virtual ICollection<ConceptRelationshipCountEntity> ConceptRelationshipCounts { get; set; }
        public virtual ICollection<RevisionRelationshipCountEntity> RevisionRelationshipCounts { get; set; }
  }
}
