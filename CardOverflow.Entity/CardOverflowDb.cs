using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using CardOverflow.Pure;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {
        public virtual DbSet<CollectedCardEntity> CollectedCard { get; set; }
        private DbSet<CollectedCardIsLatestEntity> _CollectedCardIsLatestTracked { get; set; }
        public virtual DbSet<AlphaBetaKeyEntity> AlphaBetaKey { get; set; }
        public virtual DbSet<BranchEntity> Branch { get; set; }
        public virtual DbSet<BranchInstanceEntity> BranchInstance { get; set; }
        private DbSet<BranchInstanceRelationshipCountEntity> _BranchInstanceRelationshipCountTracked { get; set; }
        private DbSet<BranchInstanceTagCountEntity> _BranchInstanceTagCountTracked { get; set; }
        public virtual DbSet<CardSettingEntity> CardSetting { get; set; }
        public virtual DbSet<CollateEntity> Collate { get; set; }
        public virtual DbSet<CollateInstanceEntity> CollateInstance { get; set; }
        public virtual DbSet<CommentCollateEntity> CommentCollate { get; set; }
        public virtual DbSet<CommentStackEntity> CommentStack { get; set; }
        public virtual DbSet<CommunalFieldEntity> CommunalField { get; set; }
        public virtual DbSet<CommunalFieldInstanceEntity> CommunalFieldInstance { get; set; }
        public virtual DbSet<CommunalFieldInstance_BranchInstanceEntity> CommunalFieldInstance_BranchInstance { get; set; }
        public virtual DbSet<DeckEntity> Deck { get; set; }
        public virtual DbSet<DeckFollowersEntity> DeckFollowers { get; set; }
        public virtual DbSet<FeedbackEntity> Feedback { get; set; }
        public virtual DbSet<FileEntity> File { get; set; }
        public virtual DbSet<File_BranchInstanceEntity> File_BranchInstance { get; set; }
        public virtual DbSet<FilterEntity> Filter { get; set; }
        public virtual DbSet<HistoryEntity> History { get; set; }
        public virtual DbSet<NotificationEntity> Notification { get; set; }
        public virtual DbSet<PotentialSignupsEntity> PotentialSignups { get; set; }
        public virtual DbSet<ReceivedNotificationEntity> ReceivedNotification { get; set; }
        public virtual DbSet<RelationshipEntity> Relationship { get; set; }
        public virtual DbSet<Relationship_CollectedCardEntity> Relationship_CollectedCard { get; set; }
        public virtual DbSet<StackEntity> Stack { get; set; }
        private DbSet<StackRelationshipCountEntity> _StackRelationshipCountTracked { get; set; }
        private DbSet<StackTagCountEntity> _StackTagCountTracked { get; set; }
        public virtual DbSet<TagEntity> Tag { get; set; }
        public virtual DbSet<Tag_CollectedCardEntity> Tag_CollectedCard { get; set; }
        public virtual DbSet<Tag_User_CollateInstanceEntity> Tag_User_CollateInstance { get; set; }
        public virtual DbSet<UserEntity> User { get; set; }
        public virtual DbSet<User_CollateInstanceEntity> User_CollateInstance { get; set; }
        public virtual DbSet<Vote_CommentCollateEntity> Vote_CommentCollate { get; set; }
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

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CollectedCardEntity>(entity =>
            {
                IfNpg(() => entity.HasIndex(e => e.TsVector).HasMethod("gin"),
                    () => entity.Ignore(e => e.TsVector));

                entity.HasIndex(e => e.BranchInstanceId);

                entity.HasIndex(e => e.CardSettingId);

                entity.HasIndex(e => e.CardState);

                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => new { e.UserId, e.BranchId });

                entity.HasIndex(e => new { e.UserId, e.StackId });

                entity.HasIndex(e => new { e.Id, e.StackId, e.UserId })
                    .IsUnique();

                entity.HasIndex(e => new { e.UserId, e.BranchInstanceId, e.Index })
                    .IsUnique();

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.CollectedCardBranches)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.BranchInstance)
                    .WithMany(p => p.CollectedCards)
                    .HasForeignKey(d => d.BranchInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CardSetting)
                    .WithMany(p => p.CollectedCards)
                    .HasForeignKey(d => d.CardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Deck)
                    .WithMany(p => p.CollectedCards)
                    .HasForeignKey(d => d.DeckId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Stack)
                    .WithMany(p => p.CollectedCards)
                    .HasForeignKey(d => d.StackId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CollectedCards)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                //entity.HasOne(d => d.BranchI)
                //    .WithMany(p => p.CollectedCardBranchIs)
                //    .HasPrincipalKey(p => new { p.BranchId, p.Id })
                //    .HasForeignKey(d => new { d.BranchId, d.BranchInstanceId })
                //    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.BranchNavigation)
                    .WithMany(p => p.CollectedCardBranchNavigations)
                    .HasPrincipalKey(p => new { p.StackId, p.Id })
                    .HasForeignKey(d => new { d.StackId, d.BranchId })
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CollectedCardIsLatestEntity>(entity =>
            {
                entity.HasMany(x => x.Tag_CollectedCards)
                    .WithOne()
                    .HasForeignKey(x => x.CollectedCardId);

                entity.ToView("collected_card_is_latest");
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

                entity.HasOne(d => d.LatestInstance)
                    .WithMany()
                    .HasForeignKey(d => d.LatestInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<BranchInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.BranchId);

                entity.HasIndex(e => e.CollateInstanceId);

                IfNpg(() => entity.HasIndex(e => e.Hash),
                    () => entity.Ignore(e => e.Hash));

                IfNpg(() => entity.HasIndex(e => e.TsVector).HasMethod("gin"),
                    () => entity.Ignore(e => e.TsVector));

                entity.HasIndex(e => new { e.Id, e.BranchId })
                    .IsUnique();

                entity.HasIndex(e => new { e.Id, e.StackId })
                    .IsUnique();

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.BranchInstances)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CollateInstance)
                    .WithMany(p => p.BranchInstances)
                    .HasForeignKey(d => d.CollateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasMany(x => x.StackTagCounts)
                     .WithOne()
                     .HasForeignKey(x => x.StackId);

                entity.HasMany(x => x.StackRelationshipCounts)
                    .WithOne()
                    .HasForeignKey(x => x.StackId);

                entity.HasMany(x => x.BranchInstanceTagCounts)
                    .WithOne()
                    .HasForeignKey(x => x.BranchInstanceId);

                entity.HasMany(x => x.BranchInstanceRelationshipCounts)
                    .WithOne()
                    .HasForeignKey(x => x.BranchInstanceId);
            });

            modelBuilder.Entity<BranchInstanceRelationshipCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.SourceBranchInstanceId, e.TargetBranchInstanceId, e.Name });

                entity.ToView("branch_instance_relationship_count");
            });

            modelBuilder.Entity<BranchInstanceTagCountEntity>(entity =>
            {
                entity.HasKey(e => new { e.BranchInstanceId, e.Name });

                entity.ToView("branch_instance_tag_count");
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

            modelBuilder.Entity<CollateEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Collates)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.LatestInstance)
                    .WithMany(p => p.Collates)
                    .HasForeignKey(d => d.LatestInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CollateInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.CollateId);

                IfNpg(() => entity.HasIndex(e => e.Hash),
                    () => entity.Ignore(e => e.Hash));

                IfNpg(() => entity.HasIndex(e => e.TsVector).HasMethod("gin"),
                    () => entity.Ignore(e => e.TsVector));

                entity.HasIndex(e => new { e.Id, e.CollateId })
                    .IsUnique();

                entity.HasOne(d => d.Collate)
                    .WithMany(p => p.CollateInstances)
                    .HasForeignKey(d => d.CollateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommentCollateEntity>(entity =>
            {
                entity.HasIndex(e => e.CollateId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Collate)
                    .WithMany(p => p.CommentCollates)
                    .HasForeignKey(d => d.CollateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentCollates)
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

            modelBuilder.Entity<CommunalFieldEntity>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.CommunalFields)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.LatestInstance)
                    .WithMany(p => p.CommunalFields)
                    .HasForeignKey(d => d.LatestInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommunalFieldInstanceEntity>(entity =>
            {
                entity.HasIndex(e => e.CommunalFieldId);

                IfNpg(() => entity.HasIndex(e => e.TsVector).HasMethod("gin"),
                    () => entity.Ignore(e => e.TsVector));

                entity.HasIndex(e => new { e.Id, e.CommunalFieldId })
                    .IsUnique();

                entity.HasOne(d => d.CommunalField)
                    .WithMany(p => p.CommunalFieldInstances)
                    .HasForeignKey(d => d.CommunalFieldId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CommunalFieldInstance_BranchInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommunalFieldInstanceId, e.BranchInstanceId });

                entity.HasIndex(e => e.BranchInstanceId);

                entity.HasOne(d => d.BranchInstance)
                    .WithMany(p => p.CommunalFieldInstance_BranchInstances)
                    .HasForeignKey(d => d.BranchInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommunalFieldInstance_BranchInstance_BranchInstanceId");

                entity.HasOne(d => d.CommunalFieldInstance)
                    .WithMany(p => p.CommunalFieldInstance_BranchInstances)
                    .HasForeignKey(d => d.CommunalFieldInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CommunalFieldInstance_BranchInstance_CommunalFieldInstanceId");
            });

            modelBuilder.Entity<DeckEntity>(entity =>
            {
                IfNpg(() => entity.HasIndex(e => e.TsVector).HasMethod("gin"),
                    () => entity.Ignore(e => e.TsVector));

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

            modelBuilder.Entity<DeckFollowersEntity>(entity =>
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

            modelBuilder.Entity<File_BranchInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.BranchInstanceId, e.FileId });

                entity.HasIndex(e => e.FileId);

                entity.HasOne(d => d.BranchInstance)
                    .WithMany(p => p.File_BranchInstances)
                    .HasForeignKey(d => d.BranchInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.File)
                    .WithMany(p => p.File_BranchInstances)
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
                entity.HasIndex(e => e.CollectedCardId);

                entity.HasOne(d => d.CollectedCard)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => d.CollectedCardId)
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
                IfNpg(() => entity.HasIndex(e => e.TsVector).HasMethod("gin"),
                    () => entity.Ignore(e => e.TsVector));
            });

            modelBuilder.Entity<Relationship_CollectedCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.SourceStackId, e.TargetStackId, e.RelationshipId, e.UserId });

                entity.HasIndex(e => e.RelationshipId);

                entity.HasIndex(e => e.SourceCollectedCardId);

                entity.HasIndex(e => e.TargetCollectedCardId);

                entity.HasOne(d => d.Relationship)
                    .WithMany(p => p.Relationship_CollectedCards)
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
                IfNpg(() => entity.HasIndex(e => e.TsVector).HasMethod("gin"),
                    () => entity.Ignore(e => e.TsVector));
            });

            modelBuilder.Entity<Tag_CollectedCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.StackId, e.TagId, e.UserId });

                entity.HasIndex(e => e.CollectedCardId);

                entity.HasIndex(e => new { e.TagId, e.StackId, e.UserId })
                    .IsUnique();

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.Tag_CollectedCards)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Tag_User_CollateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.DefaultTagId, e.CollateInstanceId, e.UserId });

                entity.HasIndex(e => e.DefaultTagId);

                entity.HasOne(d => d.DefaultTag)
                    .WithMany(p => p.Tag_User_CollateInstances)
                    .HasForeignKey(d => d.DefaultTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User_CollateInstance)
                    .WithMany(p => p.Tag_User_CollateInstances)
                    .HasForeignKey(d => new { d.UserId, d.CollateInstanceId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Tag_User_TemplatInst_User_TemplatInst_UserId_TemplatInstId");
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.DefaultCardSetting);

                entity.HasOne(d => d.DefaultDeck);
            });

            modelBuilder.Entity<User_CollateInstanceEntity>(entity =>
            {
                entity.HasKey(e => new { e.CollateInstanceId, e.UserId });

                entity.HasIndex(e => e.CollateInstanceId);

                entity.HasIndex(e => e.DefaultCardSettingId);

                entity.HasOne(d => d.CollateInstance)
                    .WithMany(p => p.User_CollateInstances)
                    .HasForeignKey(d => d.CollateInstanceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.DefaultCardSetting)
                    .WithMany(p => p.User_CollateInstances)
                    .HasForeignKey(d => d.DefaultCardSettingId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.User_CollateInstances)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Vote_CommentCollateEntity>(entity =>
            {
                entity.HasKey(e => new { e.CommentCollateId, e.UserId });

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.CommentCollate)
                    .WithMany(p => p.Vote_CommentCollates)
                    .HasForeignKey(d => d.CommentCollateId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vote_CommentCollates)
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

