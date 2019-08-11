using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FacetInstanceEntity
    {
        public FacetInstanceEntity()
        {
            Cards = new HashSet<CardEntity>();
            FieldValues = new HashSet<FieldValueEntity>();
            File_FacetInstances = new HashSet<File_FacetInstanceEntity>();
        }

        public int Id { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime? Modified { get; set; }
        public int FacetId { get; set; }
        [Required]
        [MaxLength(32)]
        public byte[] AcquireHash { get; set; }
        public bool IsDmca { get; set; }

        [ForeignKey("FacetId")]
        [InverseProperty("FacetInstances")]
        public virtual FacetEntity Facet { get; set; }
        [InverseProperty("FacetInstance")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("FacetInstance")]
        public virtual ICollection<FieldValueEntity> FieldValues { get; set; }
        [InverseProperty("FacetInstance")]
        public virtual ICollection<File_FacetInstanceEntity> File_FacetInstances { get; set; }
    }
}
