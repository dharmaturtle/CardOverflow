using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FieldValueEntity
    {
        public int ConceptInstanceId { get; set; }
        public int FieldId { get; set; }
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

        [ForeignKey("ConceptInstanceId")]
        [InverseProperty("FieldValues")]
        public virtual ConceptInstanceEntity ConceptInstance { get; set; }
        [ForeignKey("FieldId")]
        [InverseProperty("FieldValues")]
        public virtual FieldEntity Field { get; set; }
    }
}
