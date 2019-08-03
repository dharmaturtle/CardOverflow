using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class AspNetRolesEntity
    {
        public AspNetRolesEntity()
        {
            AspNetRoleClaims = new HashSet<AspNetRoleClaimsEntity>();
            AspNetUserRoles = new HashSet<AspNetUserRolesEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(256)]
        public string Name { get; set; }
        [Required]
        [StringLength(256)]
        public string NormalizedName { get; set; }
        public string ConcurrencyStamp { get; set; }

        [InverseProperty("Role")]
        public virtual ICollection<AspNetRoleClaimsEntity> AspNetRoleClaims { get; set; }
        [InverseProperty("Role")]
        public virtual ICollection<AspNetUserRolesEntity> AspNetUserRoles { get; set; }
    }
}
