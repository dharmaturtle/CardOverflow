using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {
        public virtual DbSet<AcquiredCardEntity> AcquiredCards { get; set; }
        public virtual DbSet<CardOptionEntity> CardOptions { get; set; }
        public virtual DbSet<ConceptEntity> Concepts { get; set; }
        public virtual DbSet<ConceptCommentEntity> ConceptComments { get; set; }
        public virtual DbSet<ConceptInstanceEntity> ConceptInstances { get; set; }
        public virtual DbSet<ConceptTemplateEntity> ConceptTemplates { get; set; }
        public virtual DbSet<ConceptTemplateCommentEntity> ConceptTemplateComments { get; set; }
        public virtual DbSet<ConceptTemplateConceptTemplateDefaultUserEntity> ConceptTemplateConceptTemplateDefaultUsers { get; set; }
        public virtual DbSet<ConceptTemplateDefaultEntity> ConceptTemplateDefaults { get; set; }
        public virtual DbSet<ConceptTemplateInstanceEntity> ConceptTemplateInstances { get; set; }
        public virtual DbSet<DeckEntity> Decks { get; set; }
        public virtual DbSet<FileEntity> Files { get; set; }
        public virtual DbSet<FileConceptInstanceEntity> FileConceptInstances { get; set; }
        public virtual DbSet<HistoryEntity> Histories { get; set; }
        public virtual DbSet<PrivateTagEntity> PrivateTags { get; set; }
        public virtual DbSet<PrivateTagAcquiredCardEntity> PrivateTagAcquiredCards { get; set; }
        public virtual DbSet<PublicTagEntity> PublicTags { get; set; }
        public virtual DbSet<PublicTagConceptEntity> PublicTagConcepts { get; set; }
        public virtual DbSet<UserEntity> Users { get; set; }
        public virtual DbSet<VoteConceptEntity> VoteConcepts { get; set; }
        public virtual DbSet<VoteConceptCommentEntity> VoteConceptComments { get; set; }
        public virtual DbSet<VoteConceptTemplateEntity> VoteConceptTemplates { get; set; }
        public virtual DbSet<VoteConceptTemplateCommentEntity> VoteConceptTemplateComments { get; set; }

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
                entity.HasIndex(e => e.CardOptionId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CardOption)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.CardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AcquiredCard_CardOption");

                entity.HasOne(d => d.ConceptInstance)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.ConceptInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AcquiredCard_ConceptInstance");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AcquiredCard_User");
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

            modelBuilder.Entity<ConceptEntity>(entity =>
            {
                entity.HasIndex(e => e.MaintainerId);

                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_User");
            });

            modelBuilder.Entity<ConceptCommentEntity>(entity =>
            {
                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.ConceptComments)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptComment_Concept");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ConceptComments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptComment_User");
            });

            modelBuilder.Entity<ConceptInstanceEntity>(entity =>
            {
                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.ConceptInstances)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptInstance_Concept");

                entity.HasOne(d => d.ConceptTemplateInstance)
                    .WithMany(p => p.ConceptInstances)
                    .HasForeignKey(d => d.ConceptTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptInstance_ConceptTemplateInstance");
            });

            modelBuilder.Entity<ConceptTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.MaintainerId);

                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.ConceptTemplates)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_Maintainer");
            });

            modelBuilder.Entity<ConceptTemplateCommentEntity>(entity =>
            {
                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.ConceptTemplateComments)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplateComment_ConceptTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ConceptTemplateComments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplateComment_User");
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

            modelBuilder.Entity<ConceptTemplateInstanceEntity>(entity =>
            {
                entity.Property(e => e.Css).IsUnicode(false);

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.ConceptTemplateInstances)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplateInstance_ConceptTemplate");
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
                entity.HasIndex(e => e.Sha256)
                    .HasName("AK_File_Sha256")
                    .IsUnique();
            });

            modelBuilder.Entity<FileConceptInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptInstanceId, e.FileId })
                    .HasName("PK_File_Concept");

                entity.HasIndex(e => e.FileId)
                    .HasName("IX_File_Concept_FileId");

                entity.HasOne(d => d.ConceptInstance)
                    .WithMany(p => p.FileConceptInstances)
                    .HasForeignKey(d => d.ConceptInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_File_ConceptInstance_ConceptInstance");

                entity.HasOne(d => d.File)
                    .WithMany(p => p.FileConceptInstances)
                    .HasForeignKey(d => d.FileId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_File_ConceptInstance_File");
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

            modelBuilder.Entity<PublicTagConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptId, e.TemplateIndex, e.PublicTagId });

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.PublicTagConcepts)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Concept_Concept");

                entity.HasOne(d => d.PublicTag)
                    .WithMany(p => p.PublicTagConcepts)
                    .HasForeignKey(d => d.PublicTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Concept_PublicTag");
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

            modelBuilder.Entity<VoteConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ConceptId });

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.VoteConcepts)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_Concept_Concept");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VoteConcepts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_Concept_User");
            });

            modelBuilder.Entity<VoteConceptCommentEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptCommentId, e.UserId });

                entity.HasOne(d => d.ConceptComment)
                    .WithMany(p => p.VoteConceptComments)
                    .HasForeignKey(d => d.ConceptCommentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptComment_ConceptComment");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VoteConceptComments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptComment_User");
            });

            modelBuilder.Entity<VoteConceptTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptTemplateId, e.UserId });

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.VoteConceptTemplates)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptTemplate_ConceptTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VoteConceptTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptTemplate_User");
            });

            modelBuilder.Entity<VoteConceptTemplateCommentEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptTemplateCommentId, e.UserId });

                entity.HasOne(d => d.ConceptTemplateComment)
                    .WithMany(p => p.VoteConceptTemplateComments)
                    .HasForeignKey(d => d.ConceptTemplateCommentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptTemplateComment_ConceptTemplateComment");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VoteConceptTemplateComments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptTemplateComment_User");
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}

