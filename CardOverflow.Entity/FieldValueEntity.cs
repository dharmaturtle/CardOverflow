using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FieldValueEntity
    {
        public int CardInstanceId { get; set; }
        public int FieldId { get; set; }
        [Required]
        public string Value { get; set; }

        [ForeignKey("CardInstanceId")]
        [InverseProperty("FieldValues")]
        public virtual CardInstanceEntity CardInstance { get; set; }
        [ForeignKey("FieldId")]
        [InverseProperty("FieldValues")]
        public virtual FieldEntity Field { get; set; }
    }
}
