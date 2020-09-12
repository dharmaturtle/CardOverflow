using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;
using NUlid;

namespace CardOverflow.Entity
{
    public partial class TagEntity
    {
        public TagEntity()
        {
            Tag_Cards = new HashSet<Tag_CardEntity>();
            Tag_User_Grompleafs = new HashSet<Tag_User_GrompleafEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        [Required]
        [StringLength(250)]
        public string Name {
            get => _Name;
            set {
                if (value.Length > 250) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and Name has a maximum length of 250. Attempted value: {value}");
                _Name = value;
            }
        }
        private string _Name;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }
        public NpgsqlTsVector Tsv { get; set; }

        [InverseProperty("Tag")]
        public virtual ICollection<Tag_CardEntity> Tag_Cards { get; set; }
        [InverseProperty("DefaultTag")]
        public virtual ICollection<Tag_User_GrompleafEntity> Tag_User_Grompleafs { get; set; }
    }
}
