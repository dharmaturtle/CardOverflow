using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommentConceptEntity
    {
        public CommentConceptEntity()
        {
            Vote_CommentConcepts = new HashSet<Vote_CommentConceptEntity>();
        }

        public int Id { get; set; }
        public int ConceptId { get; set; }
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

        [ForeignKey("ConceptId")]
        [InverseProperty("CommentConcepts")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("CommentConcepts")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("CommentConcept")]
        public virtual ICollection<Vote_CommentConceptEntity> Vote_CommentConcepts { get; set; }
    }
}
