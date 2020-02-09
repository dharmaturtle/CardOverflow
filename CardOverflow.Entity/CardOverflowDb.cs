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
        public virtual DbSet<AcquiredCardIsLatestEntity> AcquiredCardIsLatest { get; set; }
        public virtual DbSet<AlphaBetaKeyEntity> AlphaBetaKey { get; set; }
        public virtual DbSet<CardEntity> Card { get; set; }
        public virtual DbSet<CardInstanceEntity> CardInstance { get; set; }
        public virtual DbSet<CardSettingEntity> CardSetting { get; set; }
        public virtual DbSet<CommentCardEntity> CommentCard { get; set; }
        public virtual DbSet<CommentTemplateEntity> CommentTemplate { get; set; }
        public virtual DbSet<CommunalFieldEntity> CommunalField { get; set; }
        public virtual DbSet<CommunalFieldInstanceEntity> CommunalFieldInstance { get; set; }
        public virtual DbSet<CommunalFieldInstance_CardInstanceEntity> CommunalFieldInstance_CardInstance { get; set; }
        public virtual DbSet<FeedbackEntity> Feedback { get; set; }
        public virtual DbSet<FileEntity> File { get; set; }
        public virtual DbSet<File_CardInstanceEntity> File_CardInstance { get; set; }
        public virtual DbSet<FilterEntity> Filter { get; set; }
        public virtual DbSet<HistoryEntity> History { get; set; }
        public virtual DbSet<LatestCardInstanceEntity> LatestCardInstance { get; set; }
        public virtual DbSet<LatestCommunalFieldInstanceEntity> LatestCommunalFieldInstance { get; set; }
        public virtual DbSet<LatestTemplateInstanceEntity> LatestTemplateInstance { get; set; }
        public virtual DbSet<PotentialSignupsEntity> PotentialSignups { get; set; }
        public virtual DbSet<RelationshipEntity> Relationship { get; set; }
        public virtual DbSet<Relationship_AcquiredCardEntity> Relationship_AcquiredCard { get; set; }
        public virtual DbSet<TagEntity> Tag { get; set; }
        public virtual DbSet<Tag_AcquiredCardEntity> Tag_AcquiredCard { get; set; }
        public virtual DbSet<Tag_User_TemplateInstanceEntity> Tag_User_TemplateInstance { get; set; }
        public virtual DbSet<TemplateEntity> Template { get; set; }
        public virtual DbSet<TemplateInstanceEntity> TemplateInstance { get; set; }
        public virtual DbSet<UserEntity> User { get; set; }
        public virtual DbSet<User_TemplateInstanceEntity> User_TemplateInstance { get; set; }
        public virtual DbSet<Vote_CommentCardEntity> Vote_CommentCard { get; set; }
        public virtual DbSet<Vote_CommentTemplateEntity> Vote_CommentTemplate { get; set; }
        public virtual DbSet<Vote_FeedbackEntity> Vote_Feedback { get; set; }

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

                entity.HasIndex(e => e.CardSettingId);

                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => new { e.UserId, e.CardInstanceId })
                    .IsUnique();

                entity.HasOne(d => d.CardInstance)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.CardInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CardSetting)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.CardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AcquiredCards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<AcquiredCardIsLatestEntity>(entity =>
            {
                entity.HasMany(x => x.Tag_AcquiredCards)
                    .WithOne()
                    .HasForeignKey(x => x.AcquiredCardId);
                
                entity.ToView("AcquiredCardIsLatest");
            });

            modelBuilder.Entity<AlphaBetaKeyEntity>(entity =>
            {
                entity.HasIndex(e => e.Key)
                    .IsUnique();
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
                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => e.Hash);

                entity.HasIndex(e => e.TemplateInstanceId);

                entity.Property(e => e.Hash).IsFixedLength();

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.CardInstances)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.TemplateInstance)
                    .WithMany(p => p.CardInstances)
                    .HasForeignKey(d => d.TemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardSettingEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.LapsedCardsStepsInMinutes).IsUnicode(false);

                entity.Property(e => e.NewCardsStepsInMinutes).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CardSettings)
                    .HasForeignKey(d => d.UserId)
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

            modelBuilder.Entity<CommentTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.TemplateId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Template)
                    .WithMany(p => p.CommentTemplates)
                    .HasForeignKey(d => d.TemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommunalFieldEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.CommunalFields)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommunalFieldInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.CommunalFieldId);

                entity.HasOne(d => d.CommunalField)
                    .WithMany(p => p.CommunalFieldInstances)
                    .HasForeignKey(d => d.CommunalFieldId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommunalFieldInstance_CardInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommunalFieldInstanceId, e.CardInstanceId });

                entity.HasIndex(e => e.CardInstanceId);

                entity.HasOne(d => d.CardInstance)
                    .WithMany(p => p.CommunalFieldInstance_CardInstances)
                    .HasForeignKey(d => d.CardInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CommunalFieldInstance)
                    .WithMany(p => p.CommunalFieldInstance_CardInstances)
                    .HasForeignKey(d => d.CommunalFieldInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<FeedbackEntity>(entity =>
            {
                entity.HasIndex(e => e.ParentId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Feedbacks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasIndex(e => e.Sha256)
                    .IsUnique();

                entity.Property(e => e.Sha256).IsFixedLength();
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

            modelBuilder.Entity<FilterEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Filters)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<HistoryEntity>(entity =>
            {
                entity.HasIndex(e => e.AcquiredCardId);
            });

            modelBuilder.Entity<LatestCardInstanceEntity>(entity =>
            {
                entity.HasMany(x => x.CommunalFieldInstance_CardInstances)
                    .WithOne()
                    .HasForeignKey(x => x.CardInstanceId);
                
                entity.ToView("LatestCardInstance");
            });

            modelBuilder.Entity<LatestCommunalFieldInstanceEntity>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("LatestCommunalFieldInstance");
            });

            modelBuilder.Entity<LatestTemplateInstanceEntity>(entity =>
            {
                entity.HasMany(x => x.User_TemplateInstances)
                    .WithOne()
                    .HasForeignKey(x => x.TemplateInstanceId);

                entity.ToView("LatestTemplateInstance");

                entity.Property(e => e.Css).IsUnicode(false);
            });

            modelBuilder.Entity<RelationshipEntity>(entity =>
            {
                entity.HasIndex(e => e.SourceId);

                entity.HasIndex(e => e.TargetId);

                entity.HasIndex(e => new { e.SourceId, e.TargetId, e.Name })
                    .IsUnique();

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.RelationshipSources)
                    .HasForeignKey(d => d.SourceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Target)
                    .WithMany(p => p.RelationshipTargets)
                    .HasForeignKey(d => d.TargetId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Relationship_AcquiredCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.AcquiredCardId, e.RelationshipId });

                entity.HasIndex(e => e.RelationshipId);

                entity.HasOne(d => d.AcquiredCard)
                    .WithMany(p => p.Relationship_AcquiredCards)
                    .HasForeignKey(d => d.AcquiredCardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Relationship)
                    .WithMany(p => p.Relationship_AcquiredCards)
                    .HasForeignKey(d => d.RelationshipId)
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

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.Tag_AcquiredCards)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Tag_User_TemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.TemplateInstanceId, e.DefaultTagId });

                entity.HasIndex(e => e.DefaultTagId);

                entity.HasOne(d => d.DefaultTag)
                    .WithMany(p => p.Tag_User_TemplateInstances)
                    .HasForeignKey(d => d.DefaultTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User_TemplateInstance)
                    .WithMany(p => p.Tag_User_TemplateInstances)
                    .HasForeignKey(d => new { d.UserId, d.TemplateInstanceId })
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<TemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Templates)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<TemplateInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.Hash);

                entity.HasIndex(e => e.TemplateId);

                entity.Property(e => e.Css).IsUnicode(false);

                entity.Property(e => e.Hash).IsFixedLength();

                entity.HasOne(d => d.Template)
                    .WithMany(p => p.TemplateInstances)
                    .HasForeignKey(d => d.TemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("User");

                entity.HasIndex(e => e.DisplayName)
                    .IsUnique();

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.HasOne(d => d.DefaultCardSetting);
            });

            modelBuilder.Entity<User_TemplateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.TemplateInstanceId });

                entity.HasIndex(e => e.DefaultCardSettingId);

                entity.HasIndex(e => e.TemplateInstanceId);

                entity.HasOne(d => d.DefaultCardSetting)
                    .WithMany(p => p.User_TemplateInstances)
                    .HasForeignKey(d => d.DefaultCardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.TemplateInstance)
                    .WithMany(p => p.User_TemplateInstances)
                    .HasForeignKey(d => d.TemplateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.User_TemplateInstances)
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

            modelBuilder.Entity<Vote_CommentTemplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentTemplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentTemplate)
                    .WithMany(p => p.Vote_CommentTemplates)
                    .HasForeignKey(d => d.CommentTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Vote_FeedbackEntity>(entity =>
            {
                entity.HasKey(e => new { e.FeedbackId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Feedback)
                    .WithMany(p => p.Vote_Feedbacks)
                    .HasForeignKey(d => d.FeedbackId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_Feedbacks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

