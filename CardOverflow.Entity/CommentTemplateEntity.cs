using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentTemplateEntity
    {
        public CommentTemplateEntity()
        {
            Vote_CommentTemplates = new HashSet<Vote_CommentTemplateEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int UserId { get; set; }
        [Required]
        [StringLength(500)]
        public string Text {
            get => _Text;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Text has a maximum length of 500. Attempted value: {value}");
                _Text = value;
            }
        }
        private string _Text;
        public DateTime Created { get; set; }
        public bool IsDmca { get; set; }

        [ForeignKey("TemplateId")]
        [InverseProperty("CommentTemplates")]
        public virtual TemplateEntity Template { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentTemplates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentTemplate")]
        public virtual ICollection<Vote_CommentTemplateEntity> Vote_CommentTemplates { get; set; }
    }
}
