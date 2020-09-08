using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;

namespace ThoughtDesign.IdentityProvider.Data {
  public class IdentityConfigurationDb : ConfigurationDbContext<IdentityConfigurationDb> {
    public IdentityConfigurationDb(
        DbContextOptions<IdentityConfigurationDb> options,
        ConfigurationStoreOptions storeOptions
      ) : base(options, storeOptions) {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      base.OnModelCreating(modelBuilder);
      modelBuilder.CustomizeNames();

      modelBuilder.Entity<IdentityResourceClaim>(b => {
        b.HasOne(x => x.IdentityResource)
          .WithMany(x => x.UserClaims)
          .HasForeignKey(x => x.IdentityResourceId)
          .HasConstraintName("identityResourceClaim FK identityResource. identityResourceId")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<IdentityResourceProperty>(b => {
        b.HasOne(x => x.IdentityResource)
          .WithMany(x => x.Properties)
          .HasForeignKey(x => x.IdentityResourceId)
          .HasConstraintName("identityResourceProprty FK identityResource. identityResourceId")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });
    }

  }
}
