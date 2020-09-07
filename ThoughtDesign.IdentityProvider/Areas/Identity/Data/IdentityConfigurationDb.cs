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
      
      modelBuilder.Entity<ApiResource>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_resource_pkey");

        b.HasIndex(x => x.Name)
          .IsUnique()
          .HasName("api_resource. name. uq idx");
      });

      modelBuilder.Entity<ApiResourceClaim>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_resource_claim_pkey");

        b.HasIndex(x => x.ApiResourceId)
          .HasName("api_resource_claim. api_resource_id. idx");
      });

      modelBuilder.Entity<ApiResourceProperty>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_resource_property_pkey");

        b.HasIndex(x => x.ApiResourceId)
          .HasName("api_resource_property. api_resource_id. idx");

        b.ToTable("api_resource_property");
      });

      modelBuilder.Entity<ApiResourceScope>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_resource_scope_pkey");

        b.HasIndex(x => x.ApiResourceId)
          .HasName("api_resource_scope. api_resource_id. idx");
      });

      modelBuilder.Entity<ApiResourceSecret>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_resource_secret_pkey");

        b.HasIndex(x => x.ApiResourceId)
          .HasName("api_resource_secret. api_resource_id. idx");
      });

      modelBuilder.Entity<ApiScope>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_scope_pkey");

        b.HasIndex(x => x.Name)
          .IsUnique()
          .HasName("api_scope. name. uq idx");
      });

      modelBuilder.Entity<ApiScopeClaim>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_scope_claim_pkey");

        b.HasIndex(x => x.ScopeId)
          .HasName("api_scope_claim. scope_id. idx");
      });

      modelBuilder.Entity<ApiScopeProperty>(b => {
        b.HasKey(x => x.Id)
          .HasName("api_scope_property_pkey");

        b.HasIndex(x => x.ScopeId)
          .HasName("api_scope_property. scope_id. idx");

        b.ToTable("api_scope_property");
      });

      modelBuilder.Entity<Client>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_pkey");

        b.HasIndex(x => x.ClientId)
          .IsUnique()
          .HasName("client. client_id. uq idx");
      });

      modelBuilder.Entity<ClientClaim>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_claim_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_claim. client_id. idx");
      });

      modelBuilder.Entity<ClientCorsOrigin>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_cors_origin_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_cors_origin. client_id. idx");
      });

      modelBuilder.Entity<ClientGrantType>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_grant_type_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_grant_type. client_id. idx");
      });

      modelBuilder.Entity<ClientIdPRestriction>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_idp_restriction_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_idp_restriction. client_id. idx");

        b.ToTable("client_idp_restriction");
      });

      modelBuilder.Entity<ClientPostLogoutRedirectUri>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_post_logout_redirect_uri_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_post_logout_redirect_uri. client_id. idx");
      });

      modelBuilder.Entity<ClientProperty>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_property_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_property. client_id. idx");

        b.ToTable("client_property");
      });

      modelBuilder.Entity<ClientRedirectUri>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_redirect_uri_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_redirect_uri. client_id. idx");
      });

      modelBuilder.Entity<ClientScope>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_scope_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_scope. client_id. idx");
      });

      modelBuilder.Entity<ClientSecret>(b => {
        b.HasKey(x => x.Id)
          .HasName("client_secret_pkey");

        b.HasIndex(x => x.ClientId)
          .HasName("client_secret. client_id. idx");
      });

      modelBuilder.Entity<IdentityResource>(b => {
        b.HasKey(x => x.Id)
          .HasName("identity_resource_pkey");

        b.HasIndex(x => x.Name)
          .IsUnique()
          .HasName("identity_resource. name. uq idx");
      });

      modelBuilder.Entity<IdentityResourceClaim>(b => {
        b.HasKey(x => x.Id)
          .HasName("identity_resource_claim_pkey");

        b.HasIndex(x => x.IdentityResourceId)
          .HasName("identity_resource_claim. identity_resource_id. idx");
      });

      modelBuilder.Entity<IdentityResourceProperty>(b => {
        b.HasKey(x => x.Id)
          .HasName("identity_resource_property_pkey");

        b.HasIndex(x => x.IdentityResourceId)
          .HasName("identity_resource_property. identity_resource_id. idx");

        b.ToTable("identity_resource_property");
      });

      modelBuilder.Entity<ApiResourceClaim>(b => {
        b.HasOne<ApiResource>()
          .WithMany(x => x.UserClaims)
          .HasForeignKey(x => x.ApiResourceId)
          .HasConstraintName("api_resource_claim FK api_resource. api_resource_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ApiResourceProperty>(b => {
        b.HasOne<ApiResource>()
          .WithMany(x => x.Properties)
          .HasForeignKey(x => x.ApiResourceId)
          .HasConstraintName("api_resource_property FK api_resource. api_resource_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ApiResourceScope>(b => {
        b.HasOne<ApiResource>()
          .WithMany(x => x.Scopes)
          .HasForeignKey(x => x.ApiResourceId)
          .HasConstraintName("api_resource_scope FK api_resource. api_resource_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ApiResourceSecret>(b => {
        b.HasOne<ApiResource>()
          .WithMany(x => x.Secrets)
          .HasForeignKey(x => x.ApiResourceId)
          .HasConstraintName("api_resource_secret FK api_resource. api_resource_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ApiScopeClaim>(b => {
        b.HasOne<ApiScope>()
          .WithMany(x => x.UserClaims)
          .HasForeignKey(x => x.ScopeId)
          .HasConstraintName("api_scope_claim FK api_scope. scope_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ApiScopeProperty>(b => {
        b.HasOne<ApiScope>()
          .WithMany(x => x.Properties)
          .HasForeignKey(x => x.ScopeId)
          .HasConstraintName("api_scope_property FK api_scope. scope_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientClaim>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.Claims)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_claim FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientCorsOrigin>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.AllowedCorsOrigins)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_cors_origin FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientGrantType>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.AllowedGrantTypes)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_grant_type FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientIdPRestriction>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.IdentityProviderRestrictions)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_idp_restriction FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientPostLogoutRedirectUri>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.PostLogoutRedirectUris)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_post_logout_redirect_uri FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientProperty>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.Properties)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_property FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientRedirectUri>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.RedirectUris)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_redirect_uri FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientScope>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.AllowedScopes)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_scope FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<ClientSecret>(b => {
        b.HasOne<Client>()
          .WithMany(x => x.ClientSecrets)
          .HasForeignKey(x => x.ClientId)
          .HasConstraintName("client_secret FK client. client_id")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<IdentityResourceClaim>(b => {
        b.HasOne<IdentityResource>()
          .WithMany(x => x.UserClaims)
          .HasForeignKey(x => x.IdentityResourceId)
          .HasConstraintName("identityResourceClaim FK identityResource. identityResourceId")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });

      modelBuilder.Entity<IdentityResourceProperty>(b => {
        b.HasOne<IdentityResource>()
          .WithMany(x => x.Properties)
          .HasForeignKey(x => x.IdentityResourceId)
          .HasConstraintName("identityResourceProprty FK identityResource. identityResourceId")
          .OnDelete(DeleteBehavior.Cascade)
          .IsRequired();
      });
    }

  }
}
