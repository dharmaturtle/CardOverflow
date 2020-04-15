using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class LatestTemplateInstanceEntity
    {
        public LatestTemplateInstanceEntity()
        {
            User_TemplateInstances = new HashSet<User_TemplateInstanceEntity>();
        }

        public int AuthorId { get; set; }
        [Key]
        public int TemplateInstanceId { get; set; }
        public int TemplateId { get; set; }
        [StringLength(100)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 100. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
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
        [StringLength(500)]
        public string LatexPre {
            get => _LatexPre;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LatexPre has a maximum length of 500. Attempted value: {value}");
                _LatexPre = value;
            }
        }
        private string _LatexPre;
        [StringLength(500)]
        public string LatexPost {
            get => _LatexPost;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LatexPost has a maximum length of 500. Attempted value: {value}");
                _LatexPost = value;
            }
        }
        private string _LatexPost;
        public bool IsDmca { get; set; }
        [StringLength(4000)]
        public string QuestionTemplate {
            get => _QuestionTemplate;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and QuestionTemplate has a maximum length of 4000. Attempted value: {value}");
                _QuestionTemplate = value;
            }
        }
        private string _QuestionTemplate;
        [StringLength(4000)]
        public string AnswerTemplate {
            get => _AnswerTemplate;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and AnswerTemplate has a maximum length of 4000. Attempted value: {value}");
                _AnswerTemplate = value;
            }
        }
        private string _AnswerTemplate;
        [StringLength(200)]
        public string ShortQuestionTemplate {
            get => _ShortQuestionTemplate;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortQuestionTemplate has a maximum length of 200. Attempted value: {value}");
                _ShortQuestionTemplate = value;
            }
        }
        private string _ShortQuestionTemplate;
        [StringLength(200)]
        public string ShortAnswerTemplate {
            get => _ShortAnswerTemplate;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortAnswerTemplate has a maximum length of 200. Attempted value: {value}");
                _ShortAnswerTemplate = value;
            }
        }
        private string _ShortAnswerTemplate;
        [StringLength(4000)]
        public string Fields {
            get => _Fields;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Fields has a maximum length of 4000. Attempted value: {value}");
                _Fields = value;
            }
        }
        private string _Fields;
        [StringLength(200)]
        public string EditSummary {
            get => _EditSummary;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and EditSummary has a maximum length of 200. Attempted value: {value}");
                _EditSummary = value;
            }
        }
        private string _EditSummary;
        public long? AnkiId { get; set; }
        [ForeignKey("TemplateInstanceId")]
        public virtual TemplateInstanceEntity TemplateInstance { get; set; }
        [ForeignKey("TemplateId")]
        public virtual TemplateEntity Template { get; set; }
        public virtual ICollection<User_TemplateInstanceEntity> User_TemplateInstances { get; set; }
    }
}
