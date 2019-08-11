using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : IdentityDbContext<UserEntity, IdentityRole<int>, int>
    {
        public virtual DbSet<AcquiredCardEntity> AcquiredCard { get; set; }
        public virtual DbSet<CardEntity> Card { get; set; }
        public virtual DbSet<CardOptionEntity> CardOption { get; set; }
        public virtual DbSet<CardTemplateEntity> CardTemplate { get; set; }
        public virtual DbSet<CommentFacetEntity> CommentFacet { get; set; }
        public virtual DbSet<CommentFacetTemplateEntity> CommentFacetTemplate { get; set; }
        public virtual DbSet<ConceptEntity> Concept { get; set; }
        public virtual DbSet<DeckEntity> Deck { get; set; }
        public virtual DbSet<FacetEntity> Facet { get; set; }
        public virtual DbSet<FacetInstanceEntity> FacetInstance { get; set; }
        public virtual DbSet<FacetTemplateEntity> FacetTemplate { get; set; }
        public virtual DbSet<FacetTemplateInstanceEntity> FacetTemplateInstance { get; set; }
        public virtual DbSet<FieldEntity> Field { get; set; }
        public virtual DbSet<FieldValueEntity> FieldValue { get; set; }
        public virtual DbSet<FileEntity> File { get; set; }
        public virtual DbSet<File_FacetInstanceEntity> File_FacetInstance { get; set; }
        public virtual DbSet<HistoryEntity> History { get; set; }
        public virtual DbSet<PrivateTagEntity> PrivateTag { get; set; }
        public virtual DbSet<PrivateTag_AcquiredCardEntity> PrivateTag_AcquiredCard { get; set; }
        public virtual DbSet<PrivateTag_User_FacetTemplateInstanceEntity> PrivateTag_User_FacetTemplateInstance { get; set; }
        public virtual DbSet<PublicTagEntity> PublicTag { get; set; }
        public virtual DbSet<PublicTag_FacetEntity> PublicTag_Facet { get; set; }
        public virtual DbSet<PublicTag_User_FacetTemplateInstanceEntity> PublicTag_User_FacetTemplateInstance { get; set; }
        public virtual DbSet<UserEntity> User { get; set; }
        public virtual DbSet<User_FacetTemplateInstanceEntity> User_FacetTemplateInstance { get; set; }
        public virtual DbSet<Vote_CommentFacetEntity> Vote_CommentFacet { get; set; }
        public virtual DbSet<Vote_CommentFacetTemplateEntity> Vote_CommentFacetTemplate { get; set; }
        public virtual DbSet<Vote_FacetEntity> Vote_Facet { get; set; }
        public virtual DbSet<Vote_FacetTemplateEntity> Vote_FacetTemplate { get; set; }

        public CardOverflowDb(DbContextOptions<CardOverflowDb> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AcquiredCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.CardId });

                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => e.CardOptionId);

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AcquiredCard_Card");

                entity.HasOne(d => d.CardOption)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.CardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AcquiredCard_CardOption");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AcquiredCard_User");
            });

            modelBuilder.Entity<CardEntity>(entity =>
            {
                entity.HasIndex(e => e.CardTemplateId);

                entity.HasIndex(e => new { e.FacetInstanceId, e.CardTemplateId, e.ClozeIndex })
                    .HasName("AK_Card")
                    .IsUnique();

                entity.HasOne(d => d.CardTemplate)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.CardTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_CardTemplate");

                entity.HasOne(d => d.FacetInstance)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.FacetInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_FacetInstance");
            });

            modelBuilder.Entity<CardOptionEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .HasName("UQ_CardOption__UserId_IsDefault")
                    .IsUnique()
                    .HasFilter("([IsDefault]=(1))");

                entity.Property(e => e.LapsedCardsStepsInMinutes).IsUnicode(false);

                entity.Property(e => e.NewCardsStepsInMinutes).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CardOptions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CardOption_User");
            });

            modelBuilder.Entity<CardTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.FacetTemplateInstanceId);

                entity.HasOne(d => d.FacetTemplateInstance)
                    .WithMany(p => p.CardTemplates)
                    .HasForeignKey(d => d.FacetTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CardTemplate_FacetTemplateInstance");
            });

            modelBuilder.Entity<CommentFacetEntity>(entity =>
            {
                entity.HasIndex(e => e.FacetId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Facet)
                    .WithMany(p => p.CommentFacets)
                    .HasForeignKey(d => d.FacetId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentFacet_Facet");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentFacets)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentFacet_User");
            });

            modelBuilder.Entity<CommentFacetTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.FacetTemplateId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.FacetTemplate)
                    .WithMany(p => p.CommentFacetTemplates)
                    .HasForeignKey(d => d.FacetTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentFacetTemplate_FacetTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentFacetTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentFacetTemplate_User");
            });

            modelBuilder.Entity<ConceptEntity>(entity =>
            {
                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_User");
            });

            modelBuilder.Entity<DeckEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Decks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Deck_User");
            });

            modelBuilder.Entity<FacetEntity>(entity =>
            {
                entity.HasIndex(e => e.MaintainerId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Facets)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Facet_Concept");

                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.Facets)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Facet_User");
            });

            modelBuilder.Entity<FacetInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquireHash)
                    .HasName("AK_FacetInstance_AcquireHash")
                    .IsUnique();

                entity.HasIndex(e => e.FacetId);

                entity.HasOne(d => d.Facet)
                    .WithMany(p => p.FacetInstances)
                    .HasForeignKey(d => d.FacetId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FacetInstance_Facet");
            });

            modelBuilder.Entity<FacetTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.MaintainerId);

                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.FacetTemplates)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FacetTemplate_Maintainer");
            });

            modelBuilder.Entity<FacetTemplateInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquireHash)
                    .HasName("AK_FacetTemplateInstance_AcquireHash")
                    .IsUnique();

                entity.HasIndex(e => e.FacetTemplateId);

                entity.Property(e => e.Css).IsUnicode(false);

                entity.HasOne(d => d.FacetTemplate)
                    .WithMany(p => p.FacetTemplateInstances)
                    .HasForeignKey(d => d.FacetTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FacetTemplateInstance_FacetTemplate");
            });

            modelBuilder.Entity<FieldEntity>(entity =>
            {
                entity.HasIndex(e => e.FacetTemplateInstanceId);

                entity.HasOne(d => d.FacetTemplateInstance)
                    .WithMany(p => p.Fields)
                    .HasForeignKey(d => d.FacetTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Field_FacetTemplateInstance");
            });

            modelBuilder.Entity<FieldValueEntity>(entity =>
            {
                entity.HasKey(e => new { e.FacetInstanceId, e.FieldId });

                entity.HasIndex(e => e.FieldId);

                entity.HasOne(d => d.FacetInstance)
                    .WithMany(p => p.FieldValues)
                    .HasForeignKey(d => d.FacetInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FieldValue_FacetInstance");

                entity.HasOne(d => d.Field)
                    .WithMany(p => p.FieldValues)
                    .HasForeignKey(d => d.FieldId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FieldValue_Field");
            });

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasIndex(e => e.Sha256)
                    .HasName("AK_File_Sha256")
                    .IsUnique();
            });

            modelBuilder.Entity<File_FacetInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.FacetInstanceId, e.FileId })
                    .HasName("PK_File_Facet");

                entity.HasIndex(e => e.FileId)
                    .HasName("IX_File_Facet_FileId");

                entity.HasOne(d => d.FacetInstance)
                    .WithMany(p => p.File_FacetInstances)
                    .HasForeignKey(d => d.FacetInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_File_FacetInstance_FacetInstance");

                entity.HasOne(d => d.File)
                    .WithMany(p => p.File_FacetInstances)
                    .HasForeignKey(d => d.FileId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_File_FacetInstance_File");
            });

            modelBuilder.Entity<HistoryEntity>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.CardId });

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => new { d.UserId, d.CardId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_History_AcquiredCard");
            });

            modelBuilder.Entity<PrivateTagEntity>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.Name })
                    .HasName("AK_PrivateTag__UserId_Name")
                    .IsUnique();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.PrivateTags)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_User");
            });

            modelBuilder.Entity<PrivateTag_AcquiredCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.PrivateTagId, e.UserId, e.CardId });

                entity.HasIndex(e => new { e.UserId, e.CardId });

                entity.HasOne(d => d.PrivateTag)
                    .WithMany(p => p.PrivateTag_AcquiredCards)
                    .HasForeignKey(d => d.PrivateTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_PrivateTag");

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.PrivateTag_AcquiredCards)
                    .HasForeignKey(d => new { d.UserId, d.CardId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_AcquiredCard");
            });

            modelBuilder.Entity<PrivateTag_User_FacetTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.FacetTemplateInstanceId, e.DefaultPrivateTagId });

                entity.HasIndex(e => e.DefaultPrivateTagId);

                entity.HasOne(d => d.DefaultPrivateTag)
                    .WithMany(p => p.PrivateTag_User_FacetTemplateInstances)
                    .HasForeignKey(d => d.DefaultPrivateTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_User_FacetTemplateInstance_PrivateTag");

                entity.HasOne(d => d.User_FacetTemplateInstance)
                    .WithMany(p => p.PrivateTag_User_FacetTemplateInstances)
                    .HasForeignKey(d => new { d.UserId, d.FacetTemplateInstanceId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_User_FacetTemplateInstance_User_FacetTemplateInstance");
            });

            modelBuilder.Entity<PublicTagEntity>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("AK_PublicTag__Name")
                    .IsUnique();
            });

            modelBuilder.Entity<PublicTag_FacetEntity>(entity =>
            {
                entity.HasKey(e => new { e.FacetId, e.PublicTagId });

                entity.HasIndex(e => e.PublicTagId);

                entity.HasOne(d => d.Facet)
                    .WithMany(p => p.PublicTag_Facets)
                    .HasForeignKey(d => d.FacetId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Facet_Facet");

                entity.HasOne(d => d.PublicTag)
                    .WithMany(p => p.PublicTag_Facets)
                    .HasForeignKey(d => d.PublicTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Facet_PublicTag");
            });

            modelBuilder.Entity<PublicTag_User_FacetTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.FacetTemplateInstanceId, e.DefaultPublicTagId });

                entity.HasIndex(e => e.DefaultPublicTagId);

                entity.HasOne(d => d.DefaultPublicTag)
                    .WithMany(p => p.PublicTag_User_FacetTemplateInstances)
                    .HasForeignKey(d => d.DefaultPublicTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_User_FacetTemplateInstance_PublicTag");

                entity.HasOne(d => d.User_FacetTemplateInstance)
                    .WithMany(p => p.PublicTag_User_FacetTemplateInstances)
                    .HasForeignKey(d => new { d.UserId, d.FacetTemplateInstanceId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_User_FacetTemplateInstance_User_FacetTemplateInstance");
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("User");

                entity.HasIndex(e => e.DisplayName)
                    .HasName("AK_User__DisplayName")
                    .IsUnique();

                entity.HasIndex(e => e.Email)
                    .HasName("AK_User__Email")
                    .IsUnique();
            });

            modelBuilder.Entity<User_FacetTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.FacetTemplateInstanceId });

                entity.HasIndex(e => e.DefaultCardOptionId)
                    .HasName("IX_FacetTemplateDefault_DefaultCardOptionId");

                entity.HasIndex(e => e.FacetTemplateInstanceId);

                entity.HasOne(d => d.DefaultCardOption)
                    .WithMany(p => p.User_FacetTemplateInstances)
                    .HasForeignKey(d => d.DefaultCardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_FacetTemplateInstance_CardOption1");

                entity.HasOne(d => d.FacetTemplateInstance)
                    .WithMany(p => p.User_FacetTemplateInstances)
                    .HasForeignKey(d => d.FacetTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_FacetTemplateInstance_FacetTemplateInstance");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.User_FacetTemplateInstances)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_FacetTemplateInstance_User");
            });

            modelBuilder.Entity<Vote_CommentFacetEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentFacetId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentFacet)
                    .WithMany(p => p.Vote_CommentFacets)
                    .HasForeignKey(d => d.CommentFacetId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentFacet_CommentFacet");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentFacets)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentFacet_User");
            });

            modelBuilder.Entity<Vote_CommentFacetTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentFacetTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentFacetTemplate)
                    .WithMany(p => p.Vote_CommentFacetTemplates)
                    .HasForeignKey(d => d.CommentFacetTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentFacetTemplate_CommentFacetTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentFacetTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentFacetTemplate_User");
            });

            modelBuilder.Entity<Vote_FacetEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.FacetId });

                entity.HasIndex(e => e.FacetId);

                entity.HasOne(d => d.Facet)
                    .WithMany(p => p.Vote_Facets)
                    .HasForeignKey(d => d.FacetId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_Facet_Facet");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_Facets)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_Facet_User");
            });

            modelBuilder.Entity<Vote_FacetTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.FacetTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.FacetTemplate)
                    .WithMany(p => p.Vote_FacetTemplates)
                    .HasForeignKey(d => d.FacetTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_FacetTemplate_FacetTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_FacetTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_FacetTemplate_User");
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}

