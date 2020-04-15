using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class LatestCommunalFieldInstanceEntity
    {
        public int AuthorId { get; set; }
        public int CommunalFieldInstanceId { get; set; }
        public int CommunalFieldId { get; set; }
        [StringLength(200)]
        public string FieldName {
            get => _FieldName;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FieldName has a maximum length of 200. Attempted value: {value}");
                _FieldName = value;
            }
        }
        private string _FieldName;
        public string Value { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        [StringLength(200)]
        public string EditSummary {
            get => _EditSummary;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and EditSummary has a maximum length of 200. Attempted value: {value}");
                _EditSummary = value;
            }
        }
        private string _EditSummary;
    }
}
