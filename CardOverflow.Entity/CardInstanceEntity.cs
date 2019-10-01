using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CardInstanceEntity
    {
        public CardInstanceEntity()
        {
            AcquiredCards = new HashSet<AcquiredCardEntity>();
            File_CardInstances = new HashSet<File_CardInstanceEntity>();
        }

        public int Id { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime? Modified { get; set; }
        public int CardId { get; set; }
        [Required]
        [MaxLength(32)]
        public byte[] AcquireHash { get; set; }
        public bool IsDmca { get; set; }
        [Required]
        public string FieldValues { get; set; }
        public int CardTemplateInstanceId { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("CardInstances")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("CardTemplateInstanceId")]
        [InverseProperty("CardInstances")]
        public virtual CardTemplateInstanceEntity CardTemplateInstance { get; set; }
        [InverseProperty("CardInstance")]
        public virtual ICollection<AcquiredCardEntity> AcquiredCards { get; set; }
        [InverseProperty("CardInstance")]
        public virtual ICollection<File_CardInstanceEntity> File_CardInstances { get; set; }
    }
}
