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
        public string Name {
            get => _Name;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 100. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        [Required]
        [StringLength(100)]
        public string Font {
            get => _Font;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Font has a maximum length of 100. Attempted value: {value}");
                _Font = value;
            }
        }
        private string _Font;
        public byte FontSize { get; set; }
        public bool IsRightToLeft { get; set; }
        public byte Ordinal { get; set; }
        public bool IsSticky { get; set; }
        public int FacetTemplateInstanceId { get; set; }

        [ForeignKey("FacetTemplateInstanceId")]
        [InverseProperty("Fields")]
        public virtual FacetTemplateInstanceEntity FacetTemplateInstance { get; set; }
        [InverseProperty("Field")]
        public virtual ICollection<FieldValueEntity> FieldValues { get; set; }
    }
}
