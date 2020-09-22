using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class FeedbackEntity
    {
        public FeedbackEntity()
        {
            Children = new HashSet<FeedbackEntity>();
            Vote_Feedbacks = new HashSet<Vote_FeedbackEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [Required]
        [StringLength(50)]
        public string Title {
            get => _Title;
            set {
                if (value.Length > 50) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Title has a maximum length of 50. Attempted value: {value}");
                _Title = value;
            }
        }
        private string _Title;
        [Required]
        [StringLength(1000)]
        public string Description {
            get => _Description;
            set {
                if (value.Length > 1000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Description has a maximum length of 1000. Attempted value: {value}");
                _Description = value;
            }
        }
        private string _Description;
        public Guid UserId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        public Guid? ParentId { get; set; }
        public short? Priority { get; set; }

        [ForeignKey("ParentId")]
        [InverseProperty("Children")]
        public virtual FeedbackEntity Parent { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Feedbacks")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("Parent")]
        public virtual ICollection<FeedbackEntity> Children { get; set; }
        [InverseProperty("Feedback")]
        public virtual ICollection<Vote_FeedbackEntity> Vote_Feedbacks { get; set; }
    }
}
