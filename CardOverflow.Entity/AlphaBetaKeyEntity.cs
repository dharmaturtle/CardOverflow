using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class AlphaBetaKeyEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [Required]
        [StringLength(50)]
        public string Key {
            get => _Key;
            set {
                if (value.Length > 50) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Key has a maximum length of 50. Attempted value: {value}");
                _Key = value;
            }
        }
        private string _Key;
        public bool IsUsed { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
    }
}
