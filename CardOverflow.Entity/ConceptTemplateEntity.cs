using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptTemplate")]
    public partial class ConceptTemplateEntity
    {
        public ConceptTemplateEntity()
        {
            Concepts = new HashSet<ConceptEntity>();
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [Required]
        [StringLength(1000)]
        public string Css { get; set; }
        [Required]
        [StringLength(300)]
        public string Fields { get; set; }
        [Required]
        [StringLength(1000)]
        public string CardTemplates { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Modified { get; set; }
        public bool IsCloze { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPre { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPost { get; set; }
        [Required]
        [StringLength(100)]
        public string DefaultPublicTags { get; set; }
        [Required]
        [StringLength(100)]
        public string DefaultPrivateTags { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("ConceptTemplates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("ConceptTemplate")]
        public virtual ICollection<ConceptEntity> Concepts { get; set; }
    }
}
