using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class CommunalFieldEntity
    {
        public CommunalFieldEntity()
        {
            CommunalFieldInstances = new HashSet<CommunalFieldInstanceEntity>();
        }

        public int Id { get; set; }
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        [InverseProperty("CommunalFields")]
        public virtual UserEntity Author { get; set; }
        [InverseProperty("CommunalField")]
        public virtual ICollection<CommunalFieldInstanceEntity> CommunalFieldInstances { get; set; }
    }
}
