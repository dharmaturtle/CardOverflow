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
        public virtual DbSet<BranchEntity> Branch { get; set; }
        public virtual DbSet<LeafEntity> Leaf { get; set; }
        private DbSet<LeafRelationshipCountEntity> _LeafRelationshipCountTracked { get; set; }
        private DbSet<LeafTagCountEntity> _LeafTagCountTracked { get; set; }
        public virtual DbSet<CardSettingEntity> CardSetting { get; set; }
        public virtual DbSet<GromplateEntity> Gromplate { get; set; }
        public virtual DbSet<GrompleafEntity> Grompleaf { get; set; }
        public virtual DbSet<CommentGromplateEntity> CommentGromplate { get; set; }
        public virtual DbSet<CommentStackEntity> CommentStack { get; set; }
        public virtual DbSet<CommieldEntity> Commield { get; set; }
        public virtual DbSet<CommeafEntity> Commeaf { get; set; }
        public virtual DbSet<Commeaf_LeafEntity> Commeaf_Leaf { get; set; }
        public virtual DbSet<DeckEntity> Deck { get; set; }
        public virtual DbSet<DeckFollowerEntity> DeckFollower { get; set; }
        public virtual DbSet<FeedbackEntity> Feedback { get; set; }
        public virtual DbSet<FileEntity> File { get; set; }
        public virtual DbSet<File_LeafEntity> File_Leaf { get; set; }
        public virtual DbSet<FilterEntity> Filter { get; set; }
        public virtual DbSet<HistoryEntity> History { get; set; }
        public virtual DbSet<NotificationEntity> Notification { get; set; }
        public virtual DbSet<PotentialSignupsEntity> PotentialSignups { get; set; }
        public virtual DbSet<ReceivedNotificationEntity> ReceivedNotification { get; set; }
        public virtual DbSet<RelationshipEntity> Relationship { get; set; }
        public virtual DbSet<Relationship_CardEntity> Relationship_Card { get; set; }
        public virtual DbSet<StackEntity> Stack { get; set; }
        private DbSet<StackRelationshipCountEntity> _StackRelationshipCountTracked { get; set; }
        private DbSet<StackTagCountEntity> _StackTagCountTracked { get; set; }
        public virtual DbSet<TagEntity> Tag { get; set; }
        public virtual DbSet<Tag_CardEntity> Tag_Card { get; set; }
        public virtual DbSet<Tag_User_GrompleafEntity> Tag_User_Grompleaf { get; set; }
        public virtual DbSet<UserEntity> User { get; set; }
        public virtual DbSet<User_GrompleafEntity> User_Grompleaf { get; set; }
        public virtual DbSet<Vote_CommentGromplateEntity> Vote_CommentGromplate { get; set; }
        public virtual DbSet<Vote_CommentStackEntity> Vote_CommentStack { get; set; }
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

                entity.HasIndex(e => e.LeafId);

                entity.HasIndex(e => e.CardSettingId);

                entity.HasIndex(e => e.CardState);

                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => new { e.UserId, e.BranchId });

                entity.HasIndex(e => new { e.UserId, e.StackId });

                entity.HasIndex(e => new { e.Id, e.StackId, e.UserId })
                    .IsUnique();

                entity.HasIndex(e => new { e.UserId, e.LeafId, e.Index })
                    .IsUnique();

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.CardBranches)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Leaf)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.LeafId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CardSetting)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.CardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Deck)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.DeckId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Stack)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.StackId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                //entity.HasOne(d => d.BranchI)
                //    .WithMany(p => p.CardBranchIs)
                //    .HasPrincipalKey(p => new { p.BranchId, p.Id })
                //    .HasForeignKey(d => new { d.BranchId, d.LeafId })
                //    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.BranchNavigation)
                    .WithMany(p => p.CardBranchNavigations)
                    .HasPrincipalKey(p => new { p.StackId, p.Id })
                    .HasForeignKey(d => new { d.StackId, d.BranchId })
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CardIsLatestEntity>(entity =>
            {
                entity.HasMany(x => x.Tag_Cards)
                    .WithOne()
                    .HasForeignKey(x => x.CardId);

                entity.ToView("card_is_latest");
            });

            modelBuilder.Entity<AlphaBetaKeyEntity>(entity =>
            {
                entity.HasIndex(e => e.Key)
                    .IsUnique();
            });

            modelBuilder.Entity<BranchEntity>(entity =>
            {
                entity.HasIndex(e => new { e.Id, e.StackId })
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Branches)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Stack)
                    .WithMany(p => p.Branches)
                    .HasForeignKey(d => d.StackId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Latest)
                    .WithMany()
                    .HasForeignKey(d => d.LatestId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<LeafEntity>(entity =>
            {
                entity.HasIndex(e => e.BranchId);

                entity.HasIndex(e => e.GrompleafId);

                IfNpg(() => entity.HasIndex(e => e.Hash),
                    () => entity.Ignore(e => e.Hash));

                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));

                entity.HasIndex(e => new { e.Id, e.BranchId })
                    .IsUnique();

                entity.HasIndex(e => new { e.Id, e.StackId })
                    .IsUnique();

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.Leafs)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Grompleaf)
                    .WithMany(p => p.Leafs)
                    .HasForeignKey(d => d.GrompleafId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasMany(x => x.StackTagCounts)
                     .WithOne()
                     .HasForeignKey(x => x.StackId);

                entity.HasMany(x => x.StackRelationshipCounts)
                    .WithOne()
                    .HasForeignKey(x => x.StackId);

                entity.HasMany(x => x.LeafTagCounts)
                    .WithOne()
                    .HasForeignKey(x => x.LeafId);

                entity.HasMany(x => x.LeafRelationshipCounts)
                    .WithOne()
                    .HasForeignKey(x => x.LeafId);
            });

            modelBuilder.Entity<LeafRelationshipCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.SourceLeafId, e.TargetLeafId, e.Name });

                entity.ToView("leaf_relationship_count");
            });

            modelBuilder.Entity<LeafTagCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.LeafId, e.Name });

                entity.ToView("leaf_tag_count");
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

            modelBuilder.Entity<GromplateEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Gromplates)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Latest)
                    .WithMany(p => p.Gromplates)
                    .HasForeignKey(d => d.LatestId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<GrompleafEntity>(entity =>
            {
                entity.HasIndex(e => e.GromplateId);

                IfNpg(() => entity.HasIndex(e => e.Hash),
                    () => entity.Ignore(e => e.Hash));

                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));

                entity.HasIndex(e => new { e.Id, e.GromplateId })
                    .IsUnique();

                entity.HasOne(d => d.Gromplate)
                    .WithMany(p => p.Grompleafs)
                    .HasForeignKey(d => d.GromplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommentGromplateEntity>(entity =>
            {
                entity.HasIndex(e => e.GromplateId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Gromplate)
                    .WithMany(p => p.CommentGromplates)
                    .HasForeignKey(d => d.GromplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentGromplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommentStackEntity>(entity =>
            {
                entity.HasIndex(e => e.StackId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Stack)
                    .WithMany(p => p.CommentStacks)
                    .HasForeignKey(d => d.StackId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentStacks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommieldEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Commields)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Latest)
                    .WithMany(p => p.Commields)
                    .HasForeignKey(d => d.LatestId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommeafEntity>(entity =>
            {
                entity.HasIndex(e => e.CommieldId);

                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));

                entity.HasIndex(e => new { e.Id, e.CommieldId })
                    .IsUnique();

                entity.HasOne(d => d.Commield)
                    .WithMany(p => p.Commeafs)
                    .HasForeignKey(d => d.CommieldId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Commeaf_LeafEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommeafId, e.LeafId });

                entity.HasIndex(e => e.LeafId);

                entity.HasOne(d => d.Leaf)
                    .WithMany(p => p.Commeaf_Leafs)
                    .HasForeignKey(d => d.LeafId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Commeaf)
                    .WithMany(p => p.Commeaf_Leafs)
                    .HasForeignKey(d => d.CommeafId)
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

            modelBuilder.Entity<File_LeafEntity>(entity =>
            {
                entity.HasKey(e => new { e.LeafId, e.FileId });

                entity.HasIndex(e => e.FileId);

                entity.HasOne(d => d.Leaf)
                    .WithMany(p => p.File_Leafs)
                    .HasForeignKey(d => d.LeafId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.File)
                    .WithMany(p => p.File_Leafs)
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
                entity.HasKey(e => new { e.SourceStackId, e.TargetStackId, e.RelationshipId, e.UserId });

                entity.HasIndex(e => e.RelationshipId);

                entity.HasIndex(e => e.SourceCardId);

                entity.HasIndex(e => e.TargetCardId);

                entity.HasOne(d => d.Relationship)
                    .WithMany(p => p.Relationship_Cards)
                    .HasForeignKey(d => d.RelationshipId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<StackEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Stacks)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.DefaultBranch)
                    .WithMany()
                    .HasForeignKey(d => d.DefaultBranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<StackRelationshipCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.SourceStackId, e.TargetStackId, e.Name });

                entity.ToView("stack_relationship_count");
            });

            modelBuilder.Entity<StackTagCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.StackId, e.Name });

                entity.ToView("stack_tag_count");
            });

            modelBuilder.Entity<TagEntity>(entity =>
            {
                IfNpg(() => entity.HasIndex(e => e.Tsv).HasMethod("gin"),
                    () => entity.Ignore(e => e.Tsv));
            });

            modelBuilder.Entity<Tag_CardEntity>(entity =>
            {
                entity.HasKey(e => new { e.StackId, e.TagId, e.UserId });

                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => new { e.TagId, e.StackId, e.UserId })
                    .IsUnique();

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.Tag_Cards)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Tag_User_GrompleafEntity>(entity =>
            {
                entity.HasKey(e => new { e.DefaultTagId, e.GrompleafId, e.UserId });

                entity.HasIndex(e => e.DefaultTagId);

                entity.HasOne(d => d.DefaultTag)
                    .WithMany(p => p.Tag_User_Grompleafs)
                    .HasForeignKey(d => d.DefaultTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User_Grompleaf)
                    .WithMany(p => p.Tag_User_Grompleafs)
                    .HasForeignKey(d => new { d.UserId, d.GrompleafId })
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.DefaultCardSetting);

                entity.HasOne(d => d.DefaultDeck);
            });

            modelBuilder.Entity<User_GrompleafEntity>(entity =>
            {
                entity.HasKey(e => new { e.GrompleafId, e.UserId });

                entity.HasIndex(e => e.GrompleafId);

                entity.HasIndex(e => e.DefaultCardSettingId);

                entity.HasOne(d => d.Grompleaf)
                    .WithMany(p => p.User_Grompleafs)
                    .HasForeignKey(d => d.GrompleafId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.DefaultCardSetting)
                    .WithMany(p => p.User_Grompleafs)
                    .HasForeignKey(d => d.DefaultCardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.User_Grompleafs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Vote_CommentGromplateEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentGromplateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentGromplate)
                    .WithMany(p => p.Vote_CommentGromplates)
                    .HasForeignKey(d => d.CommentGromplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentGromplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Vote_CommentStackEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentStackId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentStack)
                    .WithMany(p => p.Vote_CommentStacks)
                    .HasForeignKey(d => d.CommentStackId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentStacks)
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

