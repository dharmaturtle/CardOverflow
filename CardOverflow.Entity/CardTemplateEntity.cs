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
        public string Name {
            get => _Name;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 100. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        [Required]
        [StringLength(500)]
        public string QuestionTemplate {
            get => _QuestionTemplate;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and QuestionTemplate has a maximum length of 500. Attempted value: {value}");
                _QuestionTemplate = value;
            }
        }
        private string _QuestionTemplate;
        [Required]
        [StringLength(500)]
        public string AnswerTemplate {
            get => _AnswerTemplate;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and AnswerTemplate has a maximum length of 500. Attempted value: {value}");
                _AnswerTemplate = value;
            }
        }
        private string _AnswerTemplate;
        [Required]
        [StringLength(100)]
        public string ShortQuestionTemplate {
            get => _ShortQuestionTemplate;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortQuestionTemplate has a maximum length of 100. Attempted value: {value}");
                _ShortQuestionTemplate = value;
            }
        }
        private string _ShortQuestionTemplate;
        [Required]
        [StringLength(100)]
        public string ShortAnswerTemplate {
            get => _ShortAnswerTemplate;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortAnswerTemplate has a maximum length of 100. Attempted value: {value}");
                _ShortAnswerTemplate = value;
            }
        }
        private string _ShortAnswerTemplate;
        public int ConceptTemplateInstanceId { get; set; }
        public byte Ordinal { get; set; }

        [ForeignKey("ConceptTemplateInstanceId")]
        [InverseProperty("CardTemplates")]
        public virtual ConceptTemplateInstanceEntity ConceptTemplateInstance { get; set; }
        [InverseProperty("CardTemplate")]
        public virtual ICollection<CardEntity> Cards { get; set; }
    }
}
