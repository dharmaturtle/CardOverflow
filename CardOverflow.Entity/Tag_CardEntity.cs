using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("tag_2_card")]
    public partial class Tag_CardEntity
    {
        [Key]
        public Guid TagId { get; set; }
        [Key]
        public Guid UserId { get; set; }
        [Key]
        public Guid StackId { get; set; }
        public Guid CardId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("Tag_Cards")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("TagId")]
        [InverseProperty("Tag_Cards")]
        public virtual TagEntity Tag { get; set; }
        [ForeignKey("StackId")]
        public virtual StackEntity Stack { get; set; }
    }
}
