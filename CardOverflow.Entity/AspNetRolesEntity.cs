using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CardOverflow.Entity
{
    public partial class AspNetRolesEntity : IdentityRole<int>
    {
        public AspNetRolesEntity()
        {
            AspNetRoleClaims = new HashSet<AspNetRoleClaimsEntity>();
            AspNetUserRoles = new HashSet<AspNetUserRolesEntity>();
        }

        [InverseProperty("Role")]
        public virtual ICollection<AspNetRoleClaimsEntity> AspNetRoleClaims { get; set; }
        [InverseProperty("Role")]
        public virtual ICollection<AspNetUserRolesEntity> AspNetUserRoles { get; set; }
    }
}
