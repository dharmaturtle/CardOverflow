using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CardOverflow.Entity
{
    public partial class AspNetUsersEntity : IdentityUser<int>
    {
        public AspNetUsersEntity()
        {
            AspNetUserClaims = new HashSet<AspNetUserClaimsEntity>();
            AspNetUserLogins = new HashSet<AspNetUserLoginsEntity>();
            AspNetUserRoles = new HashSet<AspNetUserRolesEntity>();
            AspNetUserTokens = new HashSet<AspNetUserTokensEntity>();
        }

        [InverseProperty("User")]
        public virtual ICollection<AspNetUserClaimsEntity> AspNetUserClaims { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<AspNetUserLoginsEntity> AspNetUserLogins { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<AspNetUserRolesEntity> AspNetUserRoles { get; set; }
        [InverseProperty("User")]
        public virtual ICollection<AspNetUserTokensEntity> AspNetUserTokens { get; set; }
    }
}
