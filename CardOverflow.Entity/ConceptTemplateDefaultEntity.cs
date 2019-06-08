using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("ConceptTemplateDefault")]
    public partial class ConceptTemplateDefaultEntity
    {
        public ConceptTemplateDefaultEntity()
        {
            ConceptTemplateConceptTemplateDefaultUsers = new HashSet<ConceptTemplateConceptTemplateDefaultUserEntity>();
        }

        public int Id { get; set; }
        public int DefaultCardOptionId { get; set; }
        [Required]
        [StringLength(150)]
        public string DefaultPrivateTags { get; set; }
        [Required]
        [StringLength(150)]
        public string DefaultPublicTags { get; set; }

        [ForeignKey("DefaultCardOptionId")]
        [InverseProperty("ConceptTemplateDefaults")]
        public virtual CardOptionEntity DefaultCardOption { get; set; }
        [InverseProperty("ConceptTemplateDefault")]
        public virtual ICollection<ConceptTemplateConceptTemplateDefaultUserEntity> ConceptTemplateConceptTemplateDefaultUsers { get; set; }
    }
}
