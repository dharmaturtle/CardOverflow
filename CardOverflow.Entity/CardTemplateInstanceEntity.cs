using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardTemplateInstanceEntity
    {
        public CardTemplateInstanceEntity()
        {
            CardInstances = new HashSet<CardInstanceEntity>();
            User_CardTemplateInstances = new HashSet<User_CardTemplateInstanceEntity>();
        }

        public int Id { get; set; }
        public int CardTemplateId { get; set; }
        [Required]
        [StringLength(4000)]
        public string Css {
            get => _Css;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Css has a maximum length of 4000. Attempted value: {value}");
                _Css = value;
            }
        }
        private string _Css;
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPre {
            get => _LatexPre;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LatexPre has a maximum length of 500. Attempted value: {value}");
                _LatexPre = value;
            }
        }
        private string _LatexPre;
        [Required]
        [StringLength(500)]
        public string LatexPost {
            get => _LatexPost;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LatexPost has a maximum length of 500. Attempted value: {value}");
                _LatexPost = value;
            }
        }
        private string _LatexPost;
        [Required]
        [MaxLength(32)]
        public byte[] AcquireHash { get; set; }
        public bool IsDmca { get; set; }
        [Required]
        [StringLength(4000)]
        public string QuestionTemplate {
            get => _QuestionTemplate;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and QuestionTemplate has a maximum length of 4000. Attempted value: {value}");
                _QuestionTemplate = value;
            }
        }
        private string _QuestionTemplate;
        [Required]
        [StringLength(4000)]
        public string AnswerTemplate {
            get => _AnswerTemplate;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and AnswerTemplate has a maximum length of 4000. Attempted value: {value}");
                _AnswerTemplate = value;
            }
        }
        private string _AnswerTemplate;
        [Required]
        [StringLength(200)]
        public string ShortQuestionTemplate {
            get => _ShortQuestionTemplate;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortQuestionTemplate has a maximum length of 200. Attempted value: {value}");
                _ShortQuestionTemplate = value;
            }
        }
        private string _ShortQuestionTemplate;
        [Required]
        [StringLength(200)]
        public string ShortAnswerTemplate {
            get => _ShortAnswerTemplate;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortAnswerTemplate has a maximum length of 200. Attempted value: {value}");
                _ShortAnswerTemplate = value;
            }
        }
        private string _ShortAnswerTemplate;
        [Required]
        public string Fields { get; set; }

        [ForeignKey("CardTemplateId")]
        [InverseProperty("CardTemplateInstances")]
        public virtual CardTemplateEntity CardTemplate { get; set; }
        [InverseProperty("CardTemplateInstance")]
        public virtual ICollection<CardInstanceEntity> CardInstances { get; set; }
        [InverseProperty("CardTemplateInstance")]
        public virtual ICollection<User_CardTemplateInstanceEntity> User_CardTemplateInstances { get; set; }
    }
}
