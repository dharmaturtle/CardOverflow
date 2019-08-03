using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class AspNetUsersEntity
    {
        public AspNetUsersEntity()
        {
            AspNetUserClaims = new HashSet<AspNetUserClaimsEntity>();
            AspNetUserLogins = new HashSet<AspNetUserLoginsEntity>();
            AspNetUserRoles = new HashSet<AspNetUserRolesEntity>();
            AspNetUserTokens = new HashSet<AspNetUserTokensEntity>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(256)]
        public string UserName { get; set; }
        [Required]
        [StringLength(256)]
        public string NormalizedUserName { get; set; }
        [Required]
        [StringLength(256)]
        public string Email { get; set; }
        [Required]
        [StringLength(256)]
        public string NormalizedEmail { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

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
