using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentCardTemplateEntity
    {
        public CommentCardTemplateEntity()
        {
            Vote_CommentCardTemplates = new HashSet<Vote_CommentCardTemplateEntity>();
        }

        public int Id { get; set; }
        public int CardTemplateId { get; set; }
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
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }
        public bool IsDmca { get; set; }

        [ForeignKey("CardTemplateId")]
        [InverseProperty("CommentCardTemplates")]
        public virtual CardTemplateEntity CardTemplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentCardTemplates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentCardTemplate")]
        public virtual ICollection<Vote_CommentCardTemplateEntity> Vote_CommentCardTemplates { get; set; }
    }
}
