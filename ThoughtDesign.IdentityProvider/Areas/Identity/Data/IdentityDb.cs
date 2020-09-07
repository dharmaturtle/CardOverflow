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

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      base.OnModelCreating(modelBuilder);
      modelBuilder.CustomizeNames();
    }
  }
}
