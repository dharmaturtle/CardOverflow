using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class LatestCardInstanceEntity
    {
        public int AuthorId { get; set; }
        public int CardUsers { get; set; }
        public int CardInstanceId { get; set; }
        public int CardId { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public bool IsDmca { get; set; }
        [Required]
        public string FieldValues { get; set; }
        public int CardTemplateInstanceId { get; set; }
        public int InstanceUsers { get; set; }
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
        public virtual CardTemplateInstanceEntity CardTemplateInstance { get; set; }
    }
}
