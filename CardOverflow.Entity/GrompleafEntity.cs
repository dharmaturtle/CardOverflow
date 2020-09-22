using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;
using NpgsqlTypes;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class GrompleafEntity
    {
        public GrompleafEntity()
        {
            Leafs = new HashSet<LeafEntity>();
            Gromplates = new HashSet<GromplateEntity>();
            Notifications = new HashSet<NotificationEntity>();
            User_Grompleafs = new HashSet<User_GrompleafEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [Required]
        [StringLength(100)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 100) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 100. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        public Guid GromplateId { get; set; }
        [Required]
        [StringLength(4000)]
        public string Css {
            get => _Css;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Css has a maximum length of 4000. Attempted value: {value}");
                _Css = value;
            }
        }
        private string _Css;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        [Required]
        [StringLength(500)]
        public string LatexPre {
            get => _LatexPre;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LatexPre has a maximum length of 500. Attempted value: {value}");
                _LatexPre = value;
            }
        }
        private string _LatexPre;
        [Required]
        [StringLength(500)]
        public string LatexPost {
            get => _LatexPost;
            set {
                if (value.Length > 500) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and LatexPost has a maximum length of 500. Attempted value: {value}");
                _LatexPost = value;
            }
        }
        private string _LatexPost;
        public bool IsDmca { get; set; }
        [Required]
        [StringLength(15000)]
        public string Templates
        {
            get => _Templates;
            set
            {
                if (value.Length > 15000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Templates has a maximum length of 15000. Attempted value: {value}");
                _Templates = value;
            }
        }
        private string _Templates;
        public short Type { get; set; }
        [Required]
        [StringLength(4000)]
        public string Fields {
            get => _Fields;
            set {
                if (value.Length > 4000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Fields has a maximum length of 4000. Attempted value: {value}");
                _Fields = value;
            }
        }
        private string _Fields;
        [Required]
        [StringLength(200)]
        public string EditSummary {
            get => _EditSummary;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and EditSummary has a maximum length of 200. Attempted value: {value}");
                _EditSummary = value;
            }
        }
        private string _EditSummary;
        public long? AnkiId { get; set; }
        [Required]
        [Column(TypeName = "bit(512)")]
        public BitArray Hash { get; set; }
        public string CWeightTsvHelper { get; set; }
        public NpgsqlTsVector Tsv { get; set; }

        [ForeignKey("GromplateId")]
        [InverseProperty("Grompleafs")]
        public virtual GromplateEntity Gromplate { get; set; }
        [InverseProperty("Grompleaf")]
        public virtual ICollection<LeafEntity> Leafs { get; set; }
        [InverseProperty("Latest")]
        public virtual ICollection<GromplateEntity> Gromplates { get; set; }
        [InverseProperty("Grompleaf")]
        public virtual ICollection<NotificationEntity> Notifications { get; set; }
        [InverseProperty("Grompleaf")]
        public virtual ICollection<User_GrompleafEntity> User_Grompleafs { get; set; }
    }
}
