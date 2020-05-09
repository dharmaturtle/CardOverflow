using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentCollateEntity
    {
        public CommentCollateEntity()
        {
            Vote_CommentCollates = new HashSet<Vote_CommentCollateEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int CollateId { get; set; }
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

        [ForeignKey("CollateId")]
        [InverseProperty("CommentCollates")]
        public virtual CollateEntity Collate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentCollates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentCollate")]
        public virtual ICollection<Vote_CommentCollateEntity> Vote_CommentCollates { get; set; }
    }
}
