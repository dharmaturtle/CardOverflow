using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class TemplateEntity
    {
        public TemplateEntity()
        {
            CommentTemplates = new HashSet<CommentTemplateEntity>();
            TemplateInstances = new HashSet<TemplateInstanceEntity>();
        }

        [Key]
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public bool IsListed { get; set; } = true;

        [ForeignKey("AuthorId")]
        [InverseProperty("Templates")]
        public virtual UserEntity Author { get; set; }
        [InverseProperty("Template")]
        public virtual ICollection<CommentTemplateEntity> CommentTemplates { get; set; }
        [InverseProperty("Template")]
        public virtual ICollection<TemplateInstanceEntity> TemplateInstances { get; set; }
    }
}
