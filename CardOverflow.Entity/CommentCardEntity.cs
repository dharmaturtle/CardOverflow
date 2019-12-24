using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentCardEntity
    {
        public CommentCardEntity()
        {
            Vote_CommentCards = new HashSet<Vote_CommentCardEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int CardId { get; set; }
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

        [ForeignKey("CardId")]
        [InverseProperty("CommentCards")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentCards")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentCard")]
        public virtual ICollection<Vote_CommentCardEntity> Vote_CommentCards { get; set; }
    }
}
