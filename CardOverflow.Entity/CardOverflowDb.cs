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
        public virtual DbSet<CardTemplateEntity> CardTemplates { get; set; }
        public virtual DbSet<CommentConceptEntity> CommentConcepts { get; set; }
        public virtual DbSet<CommentConceptTemplateEntity> CommentConceptTemplates { get; set; }
        public virtual DbSet<ConceptEntity> Concepts { get; set; }
        public virtual DbSet<ConceptInstanceEntity> ConceptInstances { get; set; }
        public virtual DbSet<ConceptTemplateEntity> ConceptTemplates { get; set; }
        public virtual DbSet<ConceptTemplateInstanceEntity> ConceptTemplateInstances { get; set; }
        public virtual DbSet<DeckEntity> Decks { get; set; }
        public virtual DbSet<FieldEntity> Fields { get; set; }
        public virtual DbSet<FieldValueEntity> FieldValues { get; set; }
        public virtual DbSet<FileEntity> Files { get; set; }
        public virtual DbSet<FileConceptInstanceEntity> FileConceptInstances { get; set; }
        public virtual DbSet<HistoryEntity> Histories { get; set; }
        public virtual DbSet<PrivateTagEntity> PrivateTags { get; set; }
        public virtual DbSet<PrivateTagAcquiredCardEntity> PrivateTagAcquiredCards { get; set; }
        public virtual DbSet<PublicTagEntity> PublicTags { get; set; }
        public virtual DbSet<PublicTagConceptEntity> PublicTagConcepts { get; set; }
        public virtual DbSet<UserEntity> Users { get; set; }
        public virtual DbSet<UserConceptTemplateInstanceEntity> UserConceptTemplateInstances { get; set; }
        public virtual DbSet<VoteCommentConceptEntity> VoteCommentConcepts { get; set; }
        public virtual DbSet<VoteCommentConceptTemplateEntity> VoteCommentConceptTemplates { get; set; }
        public virtual DbSet<VoteConceptEntity> VoteConcepts { get; set; }
        public virtual DbSet<VoteConceptTemplateEntity> VoteConceptTemplates { get; set; }

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

            modelBuilder.Entity<PrivateTagAcquiredCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.PrivateTagId, e.ConceptInstanceId, e.CardTemplateId, e.UserId });

                entity.HasIndex(e => new { e.UserId, e.ConceptInstanceId, e.CardTemplateId });

                entity.HasOne(d => d.PrivateTag)
                    .WithMany(p => p.PrivateTagAcquiredCards)
                    .HasForeignKey(d => d.PrivateTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_PrivateTag");

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.PrivateTagAcquiredCards)
                    .HasForeignKey(d => new { d.UserId, d.ConceptInstanceId, d.CardTemplateId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_AcquiredCard_AcquiredCard");
            });

            modelBuilder.Entity<PublicTagEntity>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("AK_PublicTag__Name")
                    .IsUnique();
            });

            modelBuilder.Entity<PublicTagConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptId, e.PublicTagId });

                entity.HasIndex(e => e.PublicTagId);

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

            modelBuilder.Entity<UserConceptTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ConceptTemplateInstanceId });

                entity.HasIndex(e => e.DefaultCardOptionId)
                    .HasName("IX_ConceptTemplateDefault_DefaultCardOptionId");

                entity.Property(e => e.DefaultPrivateTags).IsUnicode(false);

                entity.Property(e => e.DefaultPublicTags).IsUnicode(false);

                entity.HasOne(d => d.ConceptTemplateInstance)
                    .WithMany(p => p.UserConceptTemplateInstances)
                    .HasForeignKey(d => d.ConceptTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_ConceptTemplateInstance_ConceptTemplateInstance");

                entity.HasOne(d => d.DefaultCardOption)
                    .WithMany(p => p.UserConceptTemplateInstances)
                    .HasForeignKey(d => d.DefaultCardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_ConceptTemplateInstance_CardOption1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserConceptTemplateInstances)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_ConceptTemplateInstance_User");
            });

            modelBuilder.Entity<VoteCommentConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentConceptId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentConcept)
                    .WithMany(p => p.VoteCommentConcepts)
                    .HasForeignKey(d => d.CommentConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConcept_CommentConcept");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VoteCommentConcepts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConcept_User");
            });

            modelBuilder.Entity<VoteCommentConceptTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentConceptTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentConceptTemplate)
                    .WithMany(p => p.VoteCommentConceptTemplates)
                    .HasForeignKey(d => d.CommentConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConceptTemplate_CommentConceptTemplate");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VoteCommentConceptTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vote_CommentConceptTemplate_User");
            });

            modelBuilder.Entity<VoteConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ConceptId });

                entity.HasIndex(e => e.ConceptId);

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

            modelBuilder.Entity<VoteConceptTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

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

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}

