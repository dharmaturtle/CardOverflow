using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class ConceptInstanceEntity
    {
        public ConceptInstanceEntity()
        {
            Cards = new HashSet<CardEntity>();
            FieldValues = new HashSet<FieldValueEntity>();
            File_ConceptInstances = new HashSet<File_ConceptInstanceEntity>();
        }

        public int Id { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime? Modified { get; set; }
        public int ConceptId { get; set; }
        [Required]
        [MaxLength(32)]
        public byte[] AcquireHash { get; set; }

        [ForeignKey("ConceptId")]
        [InverseProperty("ConceptInstances")]
        public virtual ConceptEntity Concept { get; set; }
        [InverseProperty("ConceptInstance")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("ConceptInstance")]
        public virtual ICollection<FieldValueEntity> FieldValues { get; set; }
        [InverseProperty("ConceptInstance")]
        public virtual ICollection<File_ConceptInstanceEntity> File_ConceptInstances { get; set; }
    }
}
