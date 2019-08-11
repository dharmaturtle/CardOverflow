using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FacetTemplateInstanceEntity
    {
        public FacetTemplateInstanceEntity()
        {
            CardTemplates = new HashSet<CardTemplateEntity>();
            Fields = new HashSet<FieldEntity>();
            User_FacetTemplateInstances = new HashSet<User_FacetTemplateInstanceEntity>();
        }

        public int Id { get; set; }
        public int FacetTemplateId { get; set; }
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
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime? Modified { get; set; }
        public bool IsCloze { get; set; }
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

        [ForeignKey("FacetTemplateId")]
        [InverseProperty("FacetTemplateInstances")]
        public virtual FacetTemplateEntity FacetTemplate { get; set; }
        [InverseProperty("FacetTemplateInstance")]
        public virtual ICollection<CardTemplateEntity> CardTemplates { get; set; }
        [InverseProperty("FacetTemplateInstance")]
        public virtual ICollection<FieldEntity> Fields { get; set; }
        [InverseProperty("FacetTemplateInstance")]
        public virtual ICollection<User_FacetTemplateInstanceEntity> User_FacetTemplateInstances { get; set; }
    }
}
