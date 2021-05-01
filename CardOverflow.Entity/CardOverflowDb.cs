using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using CardOverflow.Pure;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {
        public virtual DbSet<CardEntity> Card { get; set; }
        private DbSet<CardIsLatestEntity> _CardIsLatestTracked { get; set; }
        public virtual DbSet<AlphaBetaKeyEntity> AlphaBetaKey { get; set; }
        public virtual DbSet<ExampleEntity> Example { get; set; }
        public virtual DbSet<RevisionEntity> Revision { get; set; }
        private DbSet<RevisionRelationshipCountEntity> _RevisionRelationshipCountTracked { get; set; }
        public virtual DbSet<CardSettingEntity> CardSetting { get; set; }
        public virtual DbSet<TemplateEntity> Template { get; set; }
        public virtual DbSet<TemplateRevisionEntity> TemplateRevision { get; set; }
        public virtual DbSet<CommentTemplateEntity> CommentTemplate { get; set; }
        public virtual DbSet<CommentConceptEntity> CommentConcept { get; set; }
        public virtual DbSet<DeckEntity> Deck { get; set; }
        public virtual DbSet<DeckFollowerEntity> DeckFollower { get; set; }
        public virtual DbSet<FeedbackEntity> Feedback { get; set; }
        public virtual DbSet<FileEntity> File { get; set; }
        public virtual DbSet<File_RevisionEntity> File_Revision { get; set; }
        public virtual DbSet<FilterEntity> Filter { get; set; }
        public virtual DbSet<HistoryEntity> History { get; set; }
        public virtual DbSet<NotificationEntity> Notification { get; set; }
        public virtual DbSet<PotentialSignupsEntity> PotentialSignups { get; set; }
        public virtual DbSet<ReceivedNotificationEntity> ReceivedNotification { get; set; }
        public virtual DbSet<RelationshipEntity> Relationship { get; set; }
        public virtual DbSet<Relationship_CardEntity> Relationship_Card { get; set; }
        public virtual DbSet<ConceptEntity> Concept { get; set; }
        private DbSet<ConceptRelationshipCountEntity> _ConceptRelationshipCountTracked { get; set; }
        public virtual DbSet<UserEntity> User { get; set; }
        public virtual DbSet<User_TemplateRevisionEntity> User_TemplateRevision { get; set; }
        public virtual DbSet<Vote_CommentTemplateEntity> Vote_CommentTemplate { get; set; }
        public virtual DbSet<Vote_CommentConceptEntity> Vote_CommentConcept { get; set; }
        public virtual DbSet<Vote_FeedbackEntity> Vote_Feedback { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        protected void IfNpg(Action t, Action f)
        {
            if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                t.Invoke();
            }
            else
            {
                f.Invoke();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<NotificationType>("public", "notification_type");
            modelBuilder.HasPostgresEnum<TimezoneName>("public", "timezone_name");

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CardEntity>(entity =>
            {
                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));

                entity.HasIndex(e => e.RevisionId);

                entity.HasIndex(e => e.CardSettingId);

                entity.HasIndex(e => e.CardState);

                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => new { e.UserId, e.ExampleId });

                entity.HasIndex(e => new { e.UserId, e.ConceptId });

                entity.HasIndex(e => new { e.Id, e.ConceptId, e.UserId })
                    .IsUnique();

                entity.HasIndex(e => new { e.UserId, e.RevisionId, e.Index })
                    .IsUnique();

                entity.HasOne(d => d.Example)
                    .WithMany(p => p.CardExamples)
                    .HasForeignKey(d => d.ExampleId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Revision)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.RevisionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CardSetting)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.CardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Deck)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.DeckId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                //entity.HasOne(d => d.ExampleI)
                //    .WithMany(p => p.CardExampleIs)
                //    .HasPrincipalKey(p => new { p.ExampleId, p.Id })
                //    .HasForeignKey(d => new { d.ExampleId, d.RevisionId })
                //    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.ExampleNavigation)
                    .WithMany(p => p.CardExampleNavigations)
                    .HasPrincipalKey(p => new { p.ConceptId, p.Id })
                    .HasForeignKey(d => new { d.ConceptId, d.ExampleId })
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardIsLatestEntity>(entity =>
            {
                entity.ToView("card_is_latest");
            });

            modelBuilder.Entity<AlphaBetaKeyEntity>(entity =>
            {
                entity.HasIndex(e => e.Key)
                    .IsUnique();
            });

            modelBuilder.Entity<ExampleEntity>(entity =>
            {
                entity.HasIndex(e => new { e.Id, e.ConceptId })
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Examples)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Examples)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Latest)
                    .WithMany()
                    .HasForeignKey(d => d.LatestId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<RevisionEntity>(entity =>
            {
                entity.HasIndex(e => e.ExampleId);

                entity.HasIndex(e => e.TemplateRevisionId);

                IfNpg(() => entity.HasIndex(e => e.Hash),
                    () => entity.Ignore(e => e.Hash));

                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));

                entity.HasIndex(e => new { e.Id, e.ExampleId })
                    .IsUnique();

                entity.HasIndex(e => new { e.Id, e.ConceptId })
                    .IsUnique();

                entity.HasOne(d => d.Example)
                    .WithMany(p => p.Revisions)
                    .HasForeignKey(d => d.ExampleId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.TemplateRevision)
                    .WithMany(p => p.Revisions)
                    .HasForeignKey(d => d.TemplateRevisionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasMany(x => x.ConceptRelationshipCounts)
                    .WithOne()
                    .HasForeignKey(x => x.ConceptId);

                entity.HasMany(x => x.RevisionRelationshipCounts)
                    .WithOne()
                    .HasForeignKey(x => x.RevisionId);
            });

            modelBuilder.Entity<RevisionRelationshipCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.SourceRevisionId, e.TargetRevisionId, e.Name });

                entity.ToView("revision_relationship_count");
            });

            modelBuilder.Entity<CardSettingEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => new { e.Id, e.UserId })
                    .IsUnique();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CardSettings)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<TemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Templates)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Latest)
                    .WithMany(p => p.Templates)
                    .HasForeignKey(d => d.LatestId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<TemplateRevisionEntity>(entity =>
            {
                entity.HasIndex(e => e.TemplateId);

                IfNpg(() => entity.HasIndex(e => e.Hash),
                    () => entity.Ignore(e => e.Hash));

                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));

                entity.HasIndex(e => new { e.Id, e.TemplateId })
                    .IsUnique();

                entity.HasOne(d => d.Template)
                    .WithMany(p => p.TemplateRevisions)
                    .HasForeignKey(d => d.TemplateId)
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

            modelBuilder.Entity<CommentConceptEntity>(entity =>
            {
                entity.HasIndex(e => e.ConceptId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.CommentConcepts)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentConcepts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<DeckEntity>(entity =>
            {
                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));

                entity.HasIndex(e => new { e.Id, e.UserId })
                    .IsUnique();

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.DerivedDecks)
                    .HasForeignKey(d => d.SourceId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Decks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<DeckFollowerEntity>(entity =>
            {
                entity.HasKey(e => new { e.DeckId, e.FollowerId });

                entity.HasOne(d => d.Deck)
                    .WithMany(p => p.DeckFollowers)
                    .HasForeignKey(d => d.DeckId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Follower)
                    .WithMany(p => p.DeckFollowers)
                    .HasForeignKey(d => d.FollowerId)
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
            });

            modelBuilder.Entity<File_RevisionEntity>(entity =>
            {
                entity.HasKey(e => new { e.RevisionId, e.FileId });

                entity.HasIndex(e => e.FileId);

                entity.HasOne(d => d.Revision)
                    .WithMany(p => p.File_Revisions)
                    .HasForeignKey(d => d.RevisionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.File)
                    .WithMany(p => p.File_Revisions)
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
                entity.HasIndex(e => e.CardId);

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<NotificationEntity>(entity =>
            {
                entity.HasOne(d => d.Sender)
                    .WithMany(p => p.SentNotifications)
                    .HasForeignKey(d => d.SenderId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<ReceivedNotificationEntity>(entity =>
            {
                entity.HasKey(e => new { e.NotificationId, e.ReceiverId });

                entity.HasOne(d => d.Notification)
                    .WithMany(p => p.ReceivedNotifications)
                    .HasForeignKey(d => d.NotificationId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Receiver)
                    .WithMany(p => p.ReceivedNotifications)
                    .HasForeignKey(d => d.ReceiverId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<RelationshipEntity>(entity =>
            {
                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));
            });

            modelBuilder.Entity<Relationship_CardEntity>(entity =>
            {
                entity.HasKey(e => new { e.SourceConceptId, e.TargetConceptId, e.RelationshipId, e.UserId });

                entity.HasIndex(e => e.RelationshipId);

                entity.HasIndex(e => e.SourceCardId);

                entity.HasIndex(e => e.TargetCardId);

                entity.HasOne(d => d.Relationship)
                    .WithMany(p => p.Relationship_Cards)
                    .HasForeignKey(d => d.RelationshipId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<ConceptEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.DefaultExample)
                    .WithMany()
                    .HasForeignKey(d => d.DefaultExampleId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<ConceptRelationshipCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.SourceConceptId, e.TargetConceptId, e.Name });

                entity.ToView("concept_relationship_count");
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.DefaultCardSetting);

                entity.HasOne(d => d.DefaultDeck);
            });

            modelBuilder.Entity<User_TemplateRevisionEntity>(entity =>
            {
                entity.HasKey(e => new { e.TemplateRevisionId, e.UserId });

                entity.HasIndex(e => e.TemplateRevisionId);

                entity.HasIndex(e => e.DefaultCardSettingId);

                entity.HasOne(d => d.TemplateRevision)
                    .WithMany(p => p.User_TemplateRevisions)
                    .HasForeignKey(d => d.TemplateRevisionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.DefaultCardSetting)
                    .WithMany(p => p.User_TemplateRevisions)
                    .HasForeignKey(d => d.DefaultCardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.User_TemplateRevisions)
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

            modelBuilder.Entity<Vote_CommentConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentConceptId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentConcept)
                    .WithMany(p => p.Vote_CommentConcepts)
                    .HasForeignKey(d => d.CommentConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentConcepts)
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

