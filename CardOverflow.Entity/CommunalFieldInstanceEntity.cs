using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommunalFieldInstanceEntity
    {
        public CommunalFieldInstanceEntity()
        {
            CommunalFieldInstance_CardInstances = new HashSet<CommunalFieldInstance_CardInstanceEntity>();
        }

        public int Id { get; set; }
        public int CommunalFieldId { get; set; }
        [Required]
        [StringLength(200)]
        public string FieldName {
            get => _FieldName;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FieldName has a maximum length of 200. Attempted value: {value}");
                _FieldName = value;
            }
        }
        private string _FieldName;
        [Required]
        [StringLength(500)]
        public string Value {
            get => _Value;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Value has a maximum length of 500. Attempted value: {value}");
                _Value = value;
            }
        }
        private string _Value;
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
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

        [ForeignKey("CommunalFieldId")]
        [InverseProperty("CommunalFieldInstances")]
        public virtual CommunalFieldEntity CommunalField { get; set; }
        [InverseProperty("CommunalFieldInstance")]
        public virtual ICollection<CommunalFieldInstance_CardInstanceEntity> CommunalFieldInstance_CardInstances { get; set; }
    }
}
