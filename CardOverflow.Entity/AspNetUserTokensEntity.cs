using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CardOverflow.Entity
{
    public partial class AspNetUserTokensEntity : IdentityUserToken<int>
    {
        [ForeignKey("UserId")]
        [InverseProperty("AspNetUserTokens")]
        public virtual AspNetUsersEntity User { get; set; }
    }
}
