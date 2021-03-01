using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class ConceptEntity
    {
        public ConceptEntity()
        {
            Cards = new HashSet<CardEntity>();
            Examples = new HashSet<ExampleEntity>();
            CommentConcepts = new HashSet<CommentConceptEntity>();
            Notifications = new HashSet<NotificationEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid AuthorId { get; set; }
        public int Users { get; set; }
        public Guid? CopySourceId { get; set; }
        public Guid DefaultExampleId { get; set; }
        public bool IsListed { get; set; } = true;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        [Required]
        [StringLength(300)]
        public string[] Tags { get; set; } = new string[0];
        [Required]
        public int[] TagsCount { get; set; } = new int[0];

        [ForeignKey("AuthorId")]
        [InverseProperty("Concepts")]
        public virtual UserEntity Author { get; set; }
        [ForeignKey("DefaultExampleId")]
        public virtual ExampleEntity DefaultExample { get; set; }
        [ForeignKey("CopySourceId")]
        [InverseProperty("ConceptCopySources")]
        public virtual RevisionEntity CopySource { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<CardEntity> Cards { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<ExampleEntity> Examples { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<CommentConceptEntity> CommentConcepts { get; set; }
        [InverseProperty("Concept")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
    }
}
