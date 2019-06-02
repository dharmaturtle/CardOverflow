using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("Concept")]
    public partial class ConceptEntity
    {
        public ConceptEntity()
        {
            Cards = new HashSet<CardEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(128)]
        public string Title { get; set; }
        [Required]
        [StringLength(512)]
        public string Description { get; set; }
        public int ConceptTemplateId { get; set; }
        [Required]
        public string Fields { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Modified { get; set; }

        [ForeignKey("ConceptTemplateId")]
        [InverseProperty("Concepts")]
        public virtual ConceptTemplateEntity ConceptTemplate { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<CardEntity> Cards { get; set; }
    }
}
