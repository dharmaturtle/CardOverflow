using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CardOverflow.Entity
{
    public partial class AspNetUserClaimsEntity : IdentityUserClaim<int>
    {
        [ForeignKey("UserId")]
        [InverseProperty("AspNetUserClaims")]
        public virtual AspNetUsersEntity User { get; set; }
    }
}
