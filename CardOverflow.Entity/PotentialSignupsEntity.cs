using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;

namespace CardOverflow.Entity
{
    public partial class PotentialSignupsEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [Required]
        [StringLength(500)]
        public string Email {
            get => _Email;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Email has a maximum length of 500. Attempted value: {value}");
                _Email = value;
            }
        }
        private string _Email;
        [Required]
        [StringLength(1000)]
        public string Message {
            get => _Message;
            set {
                if (value.Length > 1000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Message has a maximum length of 1000. Attempted value: {value}");
                _Message = value;
            }
        }
        private string _Message;
        public short OneIsAlpha2Beta3Ga { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
    }
}
