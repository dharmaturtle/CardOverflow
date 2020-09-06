using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;

namespace ThoughtDesign.IdentityProvider.Data {
  public class IdentityDb : IdentityDbContext<ThoughtDesignUser, IdentityRole<Guid>, Guid> {
    public IdentityDb(DbContextOptions<IdentityDb> options)
        : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder builder) {
      base.OnModelCreating(builder);
      builder.CustomizeNames();
      builder
        .Entity<IdentityRole<Guid>>(b =>
          b.HasIndex(x => x.NormalizedName).IsUnique()
            .HasName("role. normalized_name. uq idx"))
        .Entity<IdentityRoleClaim<Guid>>(b => {
          b.HasIndex(x => x.RoleId)
            .HasName("role_claim. role_id. idx");
          b.HasOne<IdentityRole<Guid>>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .HasConstraintName("role_claim to role. role_id. FK")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        })
        .Entity<IdentityUserClaim<Guid>>(b => {
          b.HasIndex(x => x.UserId)
            .HasName("user_claim. user_id. idx");
          b.HasOne<ThoughtDesignUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("user_claim to user. user_id. FK")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        })
        .Entity<IdentityUserLogin<Guid>>(b => {
          b.HasIndex(x => x.UserId).HasName("user_login. user_id. idx");
          b.HasOne<ThoughtDesignUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("user_login to user. user_id. FK")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        })
        .Entity<IdentityUserRole<Guid>>(b => {
          b.HasIndex(x => x.RoleId)
            .HasName("user_role. role_id. idx");
          b.HasOne<IdentityRole<Guid>>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .HasConstraintName("user_role to role. role_id. FK")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
          b.HasOne<ThoughtDesignUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("user_role to user. user_id. FK")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        })
        .Entity<ThoughtDesignUser>(b => {
          b.HasIndex(x => x.NormalizedEmail)
            .HasName("user. normalized_email. idx");
          b.HasIndex(x => x.NormalizedUserName).IsUnique()
            .HasName("user. normalized_user_name. uq idx");
        })
        .Entity<IdentityUserToken<Guid>>(b => b
          .HasOne<ThoughtDesignUser>()
          .WithMany()
          .HasForeignKey(x => x.UserId)
          .HasConstraintName("user_token to user. user_id. FK")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired());
    }
  }
}
