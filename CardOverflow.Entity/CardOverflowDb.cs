using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {
        public virtual DbSet<AcquiredCardEntity> AcquiredCards { get; set; }
        public virtual DbSet<CardEntity> Cards { get; set; }
        public virtual DbSet<CardOptionEntity> CardOptions { get; set; }
        public virtual DbSet<ConceptEntity> Concepts { get; set; }
        public virtual DbSet<ConceptTemplateEntity> ConceptTemplates { get; set; }
        public virtual DbSet<ConceptTemplateConceptTemplateDefaultUserEntity> ConceptTemplateConceptTemplateDefaultUsers { get; set; }
        public virtual DbSet<ConceptTemplateDefaultEntity> ConceptTemplateDefaults { get; set; }
        public virtual DbSet<DeckEntity> Decks { get; set; }
        public virtual DbSet<FileEntity> Files { get; set; }
        public virtual DbSet<HistoryEntity> Histories { get; set; }
        public virtual DbSet<PrivateTagEntity> PrivateTags { get; set; }
        public virtual DbSet<PrivateTagAcquiredCardEntity> PrivateTagAcquiredCards { get; set; }
        public virtual DbSet<PublicTagEntity> PublicTags { get; set; }
        public virtual DbSet<PublicTagCardEntity> PublicTagCards { get; set; }
        public virtual DbSet<UserEntity> Users { get; set; }

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
            modelBuilder.Entity<AcquiredCardEntity>(entity =>
            {
                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => e.CardOptionId);

                entity.HasIndex(e => e.UserId);

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
                entity.HasIndex(e => e.ConceptId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_Concept");
            });

            modelBuilder.Entity<CardOptionEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.LapsedCardsStepsInMinutes).IsUnicode(false);

                entity.Property(e => e.NewCardsStepsInMinutes).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CardOptions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CardOption_User");
            });

            modelBuilder.Entity<ConceptEntity>(entity =>
            {
                entity.HasIndex(e => e.ConceptTemplateId);

                entity.HasIndex(e => e.MaintainerId);

                entity.HasIndex(e => e.ParentId);

                entity.HasIndex(e => e.PrimaryChildId);

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_ConceptTemplate");

                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_User");

                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_Concept_Parent");

                entity.HasOne(d => d.PrimaryChild)
                    .WithMany(p => p.InversePrimaryChild_UseParentInstead)
                    .HasForeignKey(d => d.PrimaryChildId)
                    .HasConstraintName("FK_Concept_PrimaryChild");
            });

            modelBuilder.Entity<ConceptTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.MaintainerId);

                entity.HasIndex(e => e.ParentId);

                entity.HasIndex(e => e.PrimaryChildId);

                entity.Property(e => e.Css).IsUnicode(false);

                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.ConceptTemplates)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_Maintainer");

                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_ConceptTemplate_Parent");

                entity.HasOne(d => d.PrimaryChild)
                    .WithMany(p => p.InversePrimaryChild_UseParentInstead)
                    .HasForeignKey(d => d.PrimaryChildId)
                    .HasConstraintName("FK_ConceptTemplate_PrimaryChild");
            });

            modelBuilder.Entity<ConceptTemplateConceptTemplateDefaultUserEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptTemplateId, e.ConceptTemplateDefaultId, e.UserId });

                entity.HasIndex(e => e.ConceptTemplateDefaultId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.ConceptTemplateDefault)
                    .WithMany(p => p.ConceptTemplateConceptTemplateDefaultUsers)
                    .HasForeignKey(d => d.ConceptTemplateDefaultId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplateDefault");

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.ConceptTemplateConceptTemplateDefaultUsers)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_ConceptTemplateDefault_User_ConceptTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ConceptTemplateConceptTemplateDefaultUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_ConceptTemplateDefault_User_User");
            });

            modelBuilder.Entity<ConceptTemplateDefaultEntity>(entity =>
            {
                entity.HasIndex(e => e.DefaultCardOptionId);

                entity.Property(e => e.DefaultPrivateTags).IsUnicode(false);

                entity.Property(e => e.DefaultPublicTags).IsUnicode(false);

                entity.HasOne(d => d.DefaultCardOption)
                    .WithMany(p => p.ConceptTemplateDefaults)
                    .HasForeignKey(d => d.DefaultCardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplateDefault_CardOption");
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

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.FileName, e.Sha256 })
                    .HasName("AK_Media__UserId_FileName_Sha256")
                    .IsUnique();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Files)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Media_User");
            });

            modelBuilder.Entity<HistoryEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquiredCardId);

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => d.AcquiredCardId)
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

            modelBuilder.Entity<PrivateTagAcquiredCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.PrivateTagId, e.AcquiredCardId });

                entity.HasIndex(e => new { e.AcquiredCardId, e.PrivateTagId })
                    .HasName("AK_PrivateTag_AcquiredCard")
                    .IsUnique();

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.PrivateTagAcquiredCards)
                    .HasForeignKey(d => d.AcquiredCardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_AcquiredCard");

                entity.HasOne(d => d.PrivateTag)
                    .WithMany(p => p.PrivateTagAcquiredCards)
                    .HasForeignKey(d => d.PrivateTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_PrivateTag");
            });

            modelBuilder.Entity<PublicTagEntity>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("AK_PublicTag__Name")
                    .IsUnique();
            });

            modelBuilder.Entity<PublicTagCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.PublicTagId, e.CardId });

                entity.HasIndex(e => new { e.CardId, e.PublicTagId })
                    .HasName("AK_PublicTag_Card")
                    .IsUnique();

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.PublicTagCards)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Card_Card");

                entity.HasOne(d => d.PublicTag)
                    .WithMany(p => p.PublicTagCards)
                    .HasForeignKey(d => d.PublicTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Card_PublicTag");
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasIndex(e => e.DisplayName)
                    .HasName("AK_User__DisplayName")
                    .IsUnique();

                entity.HasIndex(e => e.Email)
                    .HasName("AK_User__Email")
                    .IsUnique();
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}

