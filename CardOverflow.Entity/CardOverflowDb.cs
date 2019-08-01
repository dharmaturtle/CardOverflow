using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {
        public virtual DbSet<AcquiredCardEntity> AcquiredCard { get; set; }
        public virtual DbSet<CardEntity> Card { get; set; }
        public virtual DbSet<CardOptionEntity> CardOption { get; set; }
        public virtual DbSet<CardTemplateEntity> CardTemplate { get; set; }
        public virtual DbSet<CommentConceptEntity> CommentConcept { get; set; }
        public virtual DbSet<CommentConceptTemplateEntity> CommentConceptTemplate { get; set; }
        public virtual DbSet<ConceptEntity> Concept { get; set; }
        public virtual DbSet<ConceptInstanceEntity> ConceptInstance { get; set; }
        public virtual DbSet<ConceptTemplateEntity> ConceptTemplate { get; set; }
        public virtual DbSet<ConceptTemplateInstanceEntity> ConceptTemplateInstance { get; set; }
        public virtual DbSet<DeckEntity> Deck { get; set; }
        public virtual DbSet<FieldEntity> Field { get; set; }
        public virtual DbSet<FieldValueEntity> FieldValue { get; set; }
        public virtual DbSet<FileEntity> File { get; set; }
        public virtual DbSet<File_ConceptInstanceEntity> File_ConceptInstance { get; set; }
        public virtual DbSet<HistoryEntity> History { get; set; }
        public virtual DbSet<PrivateTagEntity> PrivateTag { get; set; }
        public virtual DbSet<PrivateTag_AcquiredCardEntity> PrivateTag_AcquiredCard { get; set; }
        public virtual DbSet<PrivateTag_User_ConceptTemplateInstanceEntity> PrivateTag_User_ConceptTemplateInstance { get; set; }
        public virtual DbSet<PublicTagEntity> PublicTag { get; set; }
        public virtual DbSet<PublicTag_ConceptEntity> PublicTag_Concept { get; set; }
        public virtual DbSet<PublicTag_User_ConceptTemplateInstanceEntity> PublicTag_User_ConceptTemplateInstance { get; set; }
        public virtual DbSet<UserEntity> User { get; set; }
        public virtual DbSet<User_ConceptTemplateInstanceEntity> User_ConceptTemplateInstance { get; set; }
        public virtual DbSet<Vote_CommentConceptEntity> Vote_CommentConcept { get; set; }
        public virtual DbSet<Vote_CommentConceptTemplateEntity> Vote_CommentConceptTemplate { get; set; }
        public virtual DbSet<Vote_ConceptEntity> Vote_Concept { get; set; }
        public virtual DbSet<Vote_ConceptTemplateEntity> Vote_ConceptTemplate { get; set; }

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
                entity.HasKey(e => new { e.UserId, e.ConceptInstanceId, e.CardTemplateId });

                entity.HasIndex(e => e.CardOptionId);

                entity.HasIndex(e => new { e.ConceptInstanceId, e.CardTemplateId });

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

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => new { d.ConceptInstanceId, d.CardTemplateId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AcquiredCard_Card");
            });

            modelBuilder.Entity<CardEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptInstanceId, e.CardTemplateId });

                entity.HasIndex(e => e.CardTemplateId);

                entity.HasOne(d => d.CardTemplate)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.CardTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_CardTemplate");

                entity.HasOne(d => d.ConceptInstance)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.ConceptInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_ConceptInstance");
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
                entity.HasIndex(e => e.ConceptTemplateInstanceId);

                entity.HasOne(d => d.ConceptTemplateInstance)
                    .WithMany(p => p.CardTemplates)
                    .HasForeignKey(d => d.ConceptTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CardTemplate_ConceptTemplateInstance");
            });

            modelBuilder.Entity<CommentConceptEntity>(entity =>
            {
                entity.HasIndex(e => e.ConceptId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.CommentConcepts)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentConcept_Concept");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentConcepts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentConcept_User");
            });

            modelBuilder.Entity<CommentConceptTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.ConceptTemplateId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.CommentConceptTemplates)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentConceptTemplate_ConceptTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentConceptTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommentConceptTemplate_User");
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

            modelBuilder.Entity<ConceptInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquireHash)
                    .HasName("AK_ConceptInstance_AcquireHash")
                    .IsUnique();

                entity.HasIndex(e => e.ConceptId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.ConceptInstances)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptInstance_Concept");
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

            modelBuilder.Entity<ConceptTemplateInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquireHash)
                    .HasName("AK_ConceptTemplateInstance_AcquireHash")
                    .IsUnique();

                entity.HasIndex(e => e.ConceptTemplateId);

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

            modelBuilder.Entity<FieldEntity>(entity =>
            {
                entity.HasIndex(e => e.ConceptTemplateInstanceId);

                entity.HasOne(d => d.ConceptTemplateInstance)
                    .WithMany(p => p.Fields)
                    .HasForeignKey(d => d.ConceptTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Field_ConceptTemplateInstance");
            });

            modelBuilder.Entity<FieldValueEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptInstanceId, e.FieldId });

                entity.HasIndex(e => e.FieldId);

                entity.HasOne(d => d.ConceptInstance)
                    .WithMany(p => p.FieldValues)
                    .HasForeignKey(d => d.ConceptInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FieldValue_ConceptInstance");

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

            modelBuilder.Entity<File_ConceptInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptInstanceId, e.FileId })
                    .HasName("PK_File_Concept");

                entity.HasIndex(e => e.FileId)
                    .HasName("IX_File_Concept_FileId");

                entity.HasOne(d => d.ConceptInstance)
                    .WithMany(p => p.File_ConceptInstances)
                    .HasForeignKey(d => d.ConceptInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_File_ConceptInstance_ConceptInstance");

                entity.HasOne(d => d.File)
                    .WithMany(p => p.File_ConceptInstances)
                    .HasForeignKey(d => d.FileId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_File_ConceptInstance_File");
            });

            modelBuilder.Entity<HistoryEntity>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.ConceptInstanceId, e.CardTemplateId });

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => new { d.UserId, d.ConceptInstanceId, d.CardTemplateId })
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
                entity.HasKey(e => new { e.PrivateTagId, e.ConceptInstanceId, e.CardTemplateId, e.UserId });

                entity.HasIndex(e => new { e.UserId, e.ConceptInstanceId, e.CardTemplateId });

                entity.HasOne(d => d.PrivateTag)
                    .WithMany(p => p.PrivateTag_AcquiredCards)
                    .HasForeignKey(d => d.PrivateTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_PrivateTag");

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.PrivateTag_AcquiredCards)
                    .HasForeignKey(d => new { d.UserId, d.ConceptInstanceId, d.CardTemplateId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_AcquiredCard");
            });

            modelBuilder.Entity<PrivateTag_User_ConceptTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ConceptTemplateInstanceId, e.DefaultPrivateTagId });

                entity.HasOne(d => d.DefaultPrivateTag)
                    .WithMany(p => p.PrivateTag_User_ConceptTemplateInstances)
                    .HasForeignKey(d => d.DefaultPrivateTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_User_ConceptTemplateInstance_PrivateTag");

                entity.HasOne(d => d.User_ConceptTemplateInstance)
                    .WithMany(p => p.PrivateTag_User_ConceptTemplateInstances)
                    .HasForeignKey(d => new { d.UserId, d.ConceptTemplateInstanceId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_User_ConceptTemplateInstance_User_ConceptTemplateInstance");
            });

            modelBuilder.Entity<PublicTagEntity>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("AK_PublicTag__Name")
                    .IsUnique();
            });

            modelBuilder.Entity<PublicTag_ConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptId, e.PublicTagId });

                entity.HasIndex(e => e.PublicTagId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.PublicTag_Concepts)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Concept_Concept");

                entity.HasOne(d => d.PublicTag)
                    .WithMany(p => p.PublicTag_Concepts)
                    .HasForeignKey(d => d.PublicTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_Concept_PublicTag");
            });

            modelBuilder.Entity<PublicTag_User_ConceptTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ConceptTemplateInstanceId, e.DefaultPublicTagId });

                entity.HasOne(d => d.DefaultPublicTag)
                    .WithMany(p => p.PublicTag_User_ConceptTemplateInstances)
                    .HasForeignKey(d => d.DefaultPublicTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_User_ConceptTemplateInstance_PublicTag");

                entity.HasOne(d => d.User_ConceptTemplateInstance)
                    .WithMany(p => p.PublicTag_User_ConceptTemplateInstances)
                    .HasForeignKey(d => new { d.UserId, d.ConceptTemplateInstanceId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PublicTag_User_ConceptTemplateInstance_User_ConceptTemplateInstance");
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

            modelBuilder.Entity<User_ConceptTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ConceptTemplateInstanceId });

                entity.HasIndex(e => e.ConceptTemplateInstanceId);

                entity.HasIndex(e => e.DefaultCardOptionId)
                    .HasName("IX_ConceptTemplateDefault_DefaultCardOptionId");

                entity.HasOne(d => d.ConceptTemplateInstance)
                    .WithMany(p => p.User_ConceptTemplateInstances)
                    .HasForeignKey(d => d.ConceptTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_ConceptTemplateInstance_ConceptTemplateInstance");

                entity.HasOne(d => d.DefaultCardOption)
                    .WithMany(p => p.User_ConceptTemplateInstances)
                    .HasForeignKey(d => d.DefaultCardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_ConceptTemplateInstance_CardOption1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.User_ConceptTemplateInstances)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_ConceptTemplateInstance_User");
            });

            modelBuilder.Entity<Vote_CommentConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentConceptId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentConcept)
                    .WithMany(p => p.Vote_CommentConcepts)
                    .HasForeignKey(d => d.CommentConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConcept_CommentConcept");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentConcepts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConcept_User");
            });

            modelBuilder.Entity<Vote_CommentConceptTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentConceptTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentConceptTemplate)
                    .WithMany(p => p.Vote_CommentConceptTemplates)
                    .HasForeignKey(d => d.CommentConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConceptTemplate_CommentConceptTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentConceptTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConceptTemplate_User");
            });

            modelBuilder.Entity<Vote_ConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ConceptId });

                entity.HasIndex(e => e.ConceptId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Vote_Concepts)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_Concept_Concept");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_Concepts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_Concept_User");
            });

            modelBuilder.Entity<Vote_ConceptTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.Vote_ConceptTemplates)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptTemplate_ConceptTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_ConceptTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_ConceptTemplate_User");
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}

