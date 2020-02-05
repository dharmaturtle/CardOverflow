using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardInstanceEntity
    {
        public CardInstanceEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            CommunalFieldInstance_CardInstances = new HashSet<CommunalFieldInstance_CardInstanceEntity>();
            File_CardInstances = new HashSet<File_CardInstanceEntity>();
        }

        [Key]
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public int CardId { get; set; }
        public bool IsDmca { get; set; }
        [Required]
        public string FieldValues { get; set; }
        public int TemplateInstanceId { get; set; }
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
        public byte? AnkiNoteOrd { get; set; }
        [Required]
        [MaxLength(64)]
        public byte[] Hash { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("CardInstances")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("TemplateInstanceId")]
        [InverseProperty("CardInstances")]
        public virtual TemplateInstanceEntity TemplateInstance { get; set; }
        [InverseProperty("CardInstance")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("CardInstance")]
        public virtual ICollection<CommunalFieldInstance_CardInstanceEntity> CommunalFieldInstance_CardInstances { get; set; }
        [InverseProperty("CardInstance")]
        public virtual ICollection<File_CardInstanceEntity> File_CardInstances { get; set; }
    }
}
