using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FieldEntity
    {
        public FieldEntity()
        {
            FieldValues = new HashSet<FieldValueEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        [StringLength(100)]
        public string Font { get; set; }
        public byte FontSize { get; set; }
        public bool IsRightToLeft { get; set; }
        public byte Ordinal { get; set; }
        public bool IsSticky { get; set; }
        public int ConceptTemplateInstanceId { get; set; }

        [ForeignKey("ConceptTemplateInstanceId")]
        [InverseProperty("Fields")]
        public virtual ConceptTemplateInstanceEntity ConceptTemplateInstance { get; set; }
        [InverseProperty("Field")]
        public virtual ICollection<FieldValueEntity> FieldValues { get; set; }
    }
}
