using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class TagEntity
    {
        public TagEntity()
        {
            Tag_CollectedCards = new HashSet<Tag_CollectedCardEntity>();
            Tag_User_CollateInstances = new HashSet<Tag_User_CollateInstanceEntity>();
        }

        [Key]
        public int Id { get; set; }
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
        public NpgsqlTsVector TsVector { get; set; }

        [InverseProperty("Tag")]
        public virtual ICollection<Tag_CollectedCardEntity> Tag_CollectedCards { get; set; }
        [InverseProperty("DefaultTag")]
        public virtual ICollection<Tag_User_CollateInstanceEntity> Tag_User_CollateInstances { get; set; }
    }
}
