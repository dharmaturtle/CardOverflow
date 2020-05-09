using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class CollateInstanceEntity
    {
        public CollateInstanceEntity()
        {
            BranchInstances = new HashSet<BranchInstanceEntity>();
            Collates = new HashSet<CollateEntity>();
            User_CollateInstances = new HashSet<User_CollateInstanceEntity>();
        }

        [Key]
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
        public int CollateId { get; set; }
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
        public bool IsDmca { get; set; }
        [Required]
        [StringLength(4000)]
        public string QuestionXemplate {
            get => _QuestionXemplate;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and QuestionXemplate has a maximum length of 4000. Attempted value: {value}");
                _QuestionXemplate = value;
            }
        }
        private string _QuestionXemplate;
        [Required]
        [StringLength(4000)]
        public string AnswerXemplate {
            get => _AnswerXemplate;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and AnswerXemplate has a maximum length of 4000. Attempted value: {value}");
                _AnswerXemplate = value;
            }
        }
        private string _AnswerXemplate;
        [Required]
        [StringLength(200)]
        public string ShortQuestionXemplate {
            get => _ShortQuestionXemplate;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortQuestionXemplate has a maximum length of 200. Attempted value: {value}");
                _ShortQuestionXemplate = value;
            }
        }
        private string _ShortQuestionXemplate;
        [Required]
        [StringLength(200)]
        public string ShortAnswerXemplate {
            get => _ShortAnswerXemplate;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and ShortAnswerXemplate has a maximum length of 200. Attempted value: {value}");
                _ShortAnswerXemplate = value;
            }
        }
        private string _ShortAnswerXemplate;
        [Required]
        [StringLength(4000)]
        public string Fields {
            get => _Fields;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Fields has a maximum length of 4000. Attempted value: {value}");
                _Fields = value;
            }
        }
        private string _Fields;
        [Required]
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
        [Required]
        [Column(TypeName = "bit(512)")]
        public BitArray Hash { get; set; }
        public string CWeightTsVectorHelper { get; set; }
        public NpgsqlTsVector TsVector { get; set; }

        [ForeignKey("CollateId")]
        [InverseProperty("CollateInstances")]
        public virtual CollateEntity Collate { get; set; }
        [InverseProperty("CollateInstance")]
        public virtual ICollection<BranchInstanceEntity> BranchInstances { get; set; }
        [InverseProperty("LatestInstance")]
        public virtual ICollection<CollateEntity> Collates { get; set; }
        [InverseProperty("CollateInstance")]
        public virtual ICollection<User_CollateInstanceEntity> User_CollateInstances { get; set; }
    }
}
