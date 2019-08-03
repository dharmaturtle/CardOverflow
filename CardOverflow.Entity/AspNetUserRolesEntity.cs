using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CardOverflow.Entity
{
    public partial class AspNetUserRolesEntity : IdentityUserRole<int>
    {
        [ForeignKey("RoleId")]
        [InverseProperty("AspNetUserRoles")]
        public virtual AspNetRolesEntity Role { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("AspNetUserRoles")]
        public virtual AspNetUsersEntity User { get; set; }
    }
}
