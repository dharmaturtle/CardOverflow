using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class LatestCardInstanceEntity
    {
        public LatestCardInstanceEntity()
        {
            CommunalFieldInstance_CardInstances = new HashSet<CommunalFieldInstance_CardInstanceEntity>();
            CardInstanceTagCounts = new HashSet<CardInstanceTagCountEntity>();
            CardInstanceRelationshipCounts = new HashSet<CardInstanceRelationshipCountEntity>();
        }

        public int AuthorId { get; set; }
        public int CardUsers { get; set; }
        [Key]
        public int CardInstanceId { get; set; }
        public int CardId { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public bool IsDmca { get; set; }
        public string FieldValues { get; set; }
        public int TemplateInstanceId { get; set; }
        public int InstanceUsers { get; set; }
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
        public short? AnkiNoteOrd { get; set; }
        public virtual TemplateInstanceEntity TemplateInstance { get; set; }
        public virtual UserEntity Author { get; set; }
      
        [ForeignKey("CardId")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("CardInstanceId")]
        public virtual CardInstanceEntity CardInstance { get; set; }

        public virtual ICollection<CommunalFieldInstance_CardInstanceEntity> CommunalFieldInstance_CardInstances { get; set; }
        public virtual ICollection<CardInstanceTagCountEntity> CardInstanceTagCounts { get; set; }
        public virtual ICollection<CardInstanceRelationshipCountEntity> CardInstanceRelationshipCounts { get; set; }
    }
}
