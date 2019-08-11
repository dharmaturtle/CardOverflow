using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class PrivateTagEntity
    {
        public PrivateTagEntity()
        {
            PrivateTag_AcquiredCards = new HashSet<PrivateTag_AcquiredCardEntity>();
            PrivateTag_User_FacetTemplateInstances = new HashSet<PrivateTag_User_FacetTemplateInstanceEntity>();
        }

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
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("PrivateTags")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("PrivateTag")]
        public virtual ICollection<PrivateTag_AcquiredCardEntity> PrivateTag_AcquiredCards { get; set; }
        [InverseProperty("DefaultPrivateTag")]
        public virtual ICollection<PrivateTag_User_FacetTemplateInstanceEntity> PrivateTag_User_FacetTemplateInstances { get; set; }
    }
}
