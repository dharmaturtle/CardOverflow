using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace CardOverflow.Entity
{
    public partial class CommieldInstanceEntity
    {
        public CommieldInstanceEntity()
        {
            CommieldInstance_Leafs = new HashSet<CommieldInstance_LeafEntity>();
            Commields = new HashSet<CommieldEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int CommieldId { get; set; }
        [Required]
        [StringLength(200)]
        public string FieldName {
            get => _FieldName;
            set {
                if (value.Length > 200) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FieldName has a maximum length of 200. Attempted value: {value}");
                _FieldName = value;
            }
        }
        private string _FieldName;
        [Required]
        public string Value { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
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
        public string BWeightTsVectorHelper { get; set; }
        public NpgsqlTsVector TsVector { get; set; }

        [ForeignKey("CommieldId")]
        [InverseProperty("CommieldInstances")]
        public virtual CommieldEntity Commield { get; set; }
        [InverseProperty("CommieldInstance")]
        public virtual ICollection<CommieldInstance_LeafEntity> CommieldInstance_Leafs { get; set; }
        [InverseProperty("LatestInstance")]
        public virtual ICollection<CommieldEntity> Commields { get; set; }
    }
}
