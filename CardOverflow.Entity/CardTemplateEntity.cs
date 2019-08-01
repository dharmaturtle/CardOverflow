using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardTemplateEntity
    {
        public CardTemplateEntity()
        {
            Cards = new HashSet<CardEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        [StringLength(500)]
        public string QuestionTemplate { get; set; }
        [Required]
        [StringLength(500)]
        public string AnswerTemplate { get; set; }
        [Required]
        [StringLength(100)]
        public string ShortQuestionTemplate { get; set; }
        [Required]
        [StringLength(100)]
        public string ShortAnswerTemplate { get; set; }
        public int ConceptTemplateInstanceId { get; set; }
        public byte Ordinal { get; set; }

        [ForeignKey("ConceptTemplateInstanceId")]
        [InverseProperty("CardTemplates")]
        public virtual ConceptTemplateInstanceEntity ConceptTemplateInstance { get; set; }
        [InverseProperty("CardTemplate")]
        public virtual ICollection<CardEntity> Cards { get; set; }
    }
}
