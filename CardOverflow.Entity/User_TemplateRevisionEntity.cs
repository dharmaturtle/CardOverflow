using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CardOverflow.Entity
{
    [Table("user_2_template_revision")]
    public partial class User_TemplateRevisionEntity
    {
        [Key]
        public Guid UserId { get; set; }
        [Key]
        public Guid TemplateRevisionId { get; set; }
        public Guid DefaultCardSettingId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        [Required]
        [StringLength(300)]
        public string[] DefaultTags { get; set; } = new string[0];

        [ForeignKey("TemplateRevisionId")]
        [InverseProperty("User_TemplateRevisions")]
        public virtual TemplateRevisionEntity TemplateRevision { get; set; }
        [ForeignKey("DefaultCardSettingId")]
        [InverseProperty("User_TemplateRevisions")]
        public virtual CardSettingEntity DefaultCardSetting { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("User_TemplateRevisions")]
        public virtual UserEntity User { get; set; }
    }
}
