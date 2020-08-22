using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class FilterEntity
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        [StringLength(128)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 128) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 128. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public Guid UserId { get; set; }
        [Required]
        [StringLength(256)]
        public string Query {
            get => _Query;
            set {
                if (value.Length > 256) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Query has a maximum length of 256. Attempted value: {value}");
                _Query = value;
            }
        }
        private string _Query;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("Filters")]
        public virtual UserEntity User { get; set; }
    }
}
