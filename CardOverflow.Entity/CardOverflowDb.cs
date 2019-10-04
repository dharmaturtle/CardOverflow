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
        public virtual DbSet<AlphaBetaKeyEntity> AlphaBetaKey { get; set; }
        public virtual DbSet<CardEntity> Card { get; set; }
        public virtual DbSet<CardInstanceEntity> CardInstance { get; set; }
        public virtual DbSet<CardOptionEntity> CardOption { get; set; }
        public virtual DbSet<CardTemplateEntity> CardTemplate { get; set; }
        public virtual DbSet<CardTemplateInstanceEntity> CardTemplateInstance { get; set; }
        public virtual DbSet<CommentCardEntity> CommentCard { get; set; }
        public virtual DbSet<CommentCardTemplateEntity> CommentCardTemplate { get; set; }
        public virtual DbSet<DeckEntity> Deck { get; set; }
        public virtual DbSet<FileEntity> File { get; set; }
        public virtual DbSet<File_CardInstanceEntity> File_CardInstance { get; set; }
        public virtual DbSet<HistoryEntity> History { get; set; }
        public virtual DbSet<PotentialSignupsEntity> PotentialSignups { get; set; }
        public virtual DbSet<RelationshipEntity> Relationship { get; set; }
        public virtual DbSet<TagEntity> Tag { get; set; }
        public virtual DbSet<Tag_AcquiredCardEntity> Tag_AcquiredCard { get; set; }
        public virtual DbSet<Tag_User_CardTemplateInstanceEntity> Tag_User_CardTemplateInstance { get; set; }
        public virtual DbSet<UserEntity> User { get; set; }
        public virtual DbSet<User_CardTemplateInstanceEntity> User_CardTemplateInstance { get; set; }
        public virtual DbSet<Vote_CommentCardEntity> Vote_CommentCard { get; set; }
        public virtual DbSet<Vote_CommentCardTemplateEntity> Vote_CommentCardTemplate { get; set; }

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
                entity.HasIndex(e => e.CardInstanceId);

                entity.HasIndex(e => e.CardOptionId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CardInstance)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.CardInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CardOption)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.CardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquireHash)
                    .IsUnique();

                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => e.CardTemplateInstanceId);

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.CardInstances)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CardTemplateInstance)
                    .WithMany(p => p.CardInstances)
                    .HasForeignKey(d => d.CardTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardOptionEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .IsUnique()
                    .HasFilter("([IsDefault]=(1))");

                entity.Property(e => e.LapsedCardsStepsInMinutes).IsUnicode(false);

                entity.Property(e => e.NewCardsStepsInMinutes).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CardOptions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.CardTemplates)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardTemplateInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquireHash)
                    .IsUnique();

                entity.HasIndex(e => e.CardTemplateId);

                entity.Property(e => e.Css).IsUnicode(false);

                entity.HasOne(d => d.CardTemplate)
                    .WithMany(p => p.CardTemplateInstances)
                    .HasForeignKey(d => d.CardTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommentCardEntity>(entity =>
            {
                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.CommentCards)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentCards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommentCardTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.CardTemplateId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CardTemplate)
                    .WithMany(p => p.CommentCardTemplates)
                    .HasForeignKey(d => d.CardTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentCardTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<DeckEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Decks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasIndex(e => e.Sha256)
                    .IsUnique();
            });

            modelBuilder.Entity<File_CardInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.CardInstanceId, e.FileId });

                entity.HasIndex(e => e.FileId);

                entity.HasOne(d => d.CardInstance)
                    .WithMany(p => p.File_CardInstances)
                    .HasForeignKey(d => d.CardInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.File)
                    .WithMany(p => p.File_CardInstances)
                    .HasForeignKey(d => d.FileId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<HistoryEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquiredCardId);

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => d.AcquiredCardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<RelationshipEntity>(entity =>
            {
                entity.HasIndex(e => e.SourceId);

                entity.HasIndex(e => e.TargetId);

                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => new { e.SourceId, e.TargetId, e.UserId, e.Name })
                    .IsUnique();

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.RelationshipSources)
                    .HasForeignKey(d => d.SourceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Target)
                    .WithMany(p => p.RelationshipTargets)
                    .HasForeignKey(d => d.TargetId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Relationships)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<TagEntity>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<Tag_AcquiredCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.TagId, e.AcquiredCardId });

                entity.HasIndex(e => e.AcquiredCardId);

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.Tag_AcquiredCards)
                    .HasForeignKey(d => d.AcquiredCardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.Tag_AcquiredCards)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Tag_User_CardTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.CardTemplateInstanceId, e.DefaultTagId });

                entity.HasIndex(e => e.DefaultTagId);

                entity.HasOne(d => d.DefaultTag)
                    .WithMany(p => p.Tag_User_CardTemplateInstances)
                    .HasForeignKey(d => d.DefaultTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User_CardTemplateInstance)
                    .WithMany(p => p.Tag_User_CardTemplateInstances)
                    .HasForeignKey(d => new { d.UserId, d.CardTemplateInstanceId })
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("User");

                entity.HasIndex(e => e.DisplayName)
                    .IsUnique();

                entity.HasIndex(e => e.Email)
                    .IsUnique();
            });

            modelBuilder.Entity<User_CardTemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.CardTemplateInstanceId });

                entity.HasIndex(e => e.CardTemplateInstanceId);

                entity.HasIndex(e => e.DefaultCardOptionId);

                entity.HasOne(d => d.CardTemplateInstance)
                    .WithMany(p => p.User_CardTemplateInstances)
                    .HasForeignKey(d => d.CardTemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.DefaultCardOption)
                    .WithMany(p => p.User_CardTemplateInstances)
                    .HasForeignKey(d => d.DefaultCardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.User_CardTemplateInstances)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Vote_CommentCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentCardId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentCard)
                    .WithMany(p => p.Vote_CommentCards)
                    .HasForeignKey(d => d.CommentCardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentCards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Vote_CommentCardTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentCardTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentCardTemplate)
                    .WithMany(p => p.Vote_CommentCardTemplates)
                    .HasForeignKey(d => d.CommentCardTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentCardTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}

