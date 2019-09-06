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
            CardTemplateInstances = new HashSet<CardTemplateInstanceEntity>();
            CommentCardTemplates = new HashSet<CommentCardTemplateEntity>();
        }

        public int Id { get; set; }
        public int AuthorId { get; set; }
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

        [ForeignKey("AuthorId")]
        [InverseProperty("CardTemplates")]
        public virtual UserEntity Author { get; set; }
        [InverseProperty("CardTemplate")]
        public virtual ICollection<CardTemplateInstanceEntity> CardTemplateInstances { get; set; }
        [InverseProperty("CardTemplate")]
        public virtual ICollection<CommentCardTemplateEntity> CommentCardTemplates { get; set; }
    }
}
