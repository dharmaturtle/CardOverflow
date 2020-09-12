using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;

namespace CardOverflow.Entity
{
    public partial class CommentStackEntity
    {
        public CommentStackEntity()
        {
            Vote_CommentStacks = new HashSet<Vote_CommentStackEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid StackId { get; set; }
        public Guid UserId { get; set; }
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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public bool IsDmca { get; set; }

        [ForeignKey("StackId")]
        [InverseProperty("CommentStacks")]
        public virtual StackEntity Stack { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentStacks")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentStack")]
        public virtual ICollection<Vote_CommentStackEntity> Vote_CommentStacks { get; set; }
    }
}
