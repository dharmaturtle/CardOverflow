using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class CommentGromplateEntity
    {
        public CommentGromplateEntity()
        {
            Vote_CommentGromplates = new HashSet<Vote_CommentGromplateEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid GromplateId { get; set; }
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
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        public bool IsDmca { get; set; }

        [ForeignKey("GromplateId")]
        [InverseProperty("CommentGromplates")]
        public virtual GromplateEntity Gromplate { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentGromplates")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentGromplate")]
        public virtual ICollection<Vote_CommentGromplateEntity> Vote_CommentGromplates { get; set; }
    }
}
