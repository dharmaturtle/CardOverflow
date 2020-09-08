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
  public class IdentityPersistedGrantDb : PersistedGrantDbContext<IdentityPersistedGrantDb> {
    public IdentityPersistedGrantDb(
        DbContextOptions<IdentityPersistedGrantDb> options,
        OperationalStoreOptions storeOptions
      ) : base(options, storeOptions) {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      base.OnModelCreating(modelBuilder);
      modelBuilder.CustomizeNames();
    }

  }
}
