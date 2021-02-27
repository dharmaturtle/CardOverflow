using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class ConceptRelationshipCountEntity
    {
        public Guid SourceConceptId { get; set; }
        public Guid TargetConceptId { get; set; }
        public Guid ConceptId { get; set; }
        [StringLength(250)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 250) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 250. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public int Count { get; set; }
    }
}
