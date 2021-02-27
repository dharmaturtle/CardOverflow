using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class ExampleEntity
    {
        public ExampleEntity()
        {
            CardExampleNavigations = new HashSet<CardEntity>();
            CardExamples = new HashSet<CardEntity>();
            Leafs = new HashSet<LeafEntity>();
            NotificationExamples = new HashSet<NotificationEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [StringLength(64)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 64) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 64. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public Guid AuthorId { get; set; }
        public Guid ConceptId { get; set; }
        public Guid LatestId { get; set; }
        public int Users { get; set; }
        public bool IsListed { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        [Required]
        [StringLength(300)]
        public string[] Tags { get; set; } = new string[0];
        [Required]
        public int[] TagsCount { get; set; } = new int[0];

        [ForeignKey("AuthorId")]
        [InverseProperty("Examples")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("LatestId")]
        public virtual LeafEntity Latest { get; set; }
        [ForeignKey("ConceptId")]
        [InverseProperty("Examples")]
        public virtual ConceptEntity Concept { get; set; }
        public virtual ICollection<CardEntity> CardExampleNavigations { get; set; }
        [InverseProperty("Example")]
        public virtual ICollection<CardEntity> CardExamples { get; set; }
        [InverseProperty("Example")]
        public virtual ICollection<LeafEntity> Leafs { get; set; }
        [InverseProperty("Example")]
        public virtual ICollection<NotificationEntity> NotificationExamples { get; set; }
    }
}
