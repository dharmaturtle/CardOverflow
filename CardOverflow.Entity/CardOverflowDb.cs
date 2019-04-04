using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {
        public virtual DbSet<CardEntity> Cards { get; set; }
        public virtual DbSet<CardOptionEntity> CardOptions { get; set; }
        public virtual DbSet<ConceptEntity> Concepts { get; set; }
        public virtual DbSet<ConceptTagUserEntity> ConceptTagUsers { get; set; }
        public virtual DbSet<DeckEntity> Decks { get; set; }
        public virtual DbSet<DeckCardEntity> DeckCards { get; set; }
        public virtual DbSet<DeckTagEntity> DeckTags { get; set; }
        public virtual DbSet<HistoryEntity> Histories { get; set; }
        public virtual DbSet<TagEntity> Tags { get; set; }
        public virtual DbSet<UserEntity> Users { get; set; }

        public CardOverflowDb(DbContextOptions options) : base(options)
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
            modelBuilder.Entity<CardEntity>(entity =>
            {
                entity.ToTable("Card");

                entity.HasIndex(e => e.ConceptId);

                entity.Property(e => e.Answer)
                    .IsRequired()
                    .HasMaxLength(1028);

                entity.Property(e => e.Modified).HasColumnType("smalldatetime");

                entity.Property(e => e.Question)
                    .IsRequired()
                    .HasMaxLength(1028);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_Concept");
            });

            modelBuilder.Entity<CardOptionEntity>(entity =>
            {
                entity.Property(e => e.LapsedCardsSteps)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.NewCardsSteps)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CardOptions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CardOptions_User");
            });

            modelBuilder.Entity<ConceptEntity>(entity =>
            {
                entity.ToTable("Concept");

                entity.Property(e => e.Description).HasMaxLength(512);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(128);
            });

            modelBuilder.Entity<ConceptTagUserEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptId, e.TagId, e.UserId });

                entity.ToTable("Concept_Tag_User");

                entity.HasIndex(e => e.TagId);

                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.ConceptTagUsers)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_Tag_User_Concept");

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.ConceptTagUsers)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_Tag_User_Tag");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ConceptTagUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_Tag_User_User");
            });

            modelBuilder.Entity<DeckEntity>(entity =>
            {
                entity.ToTable("Deck");

                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Decks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Deck_User");
            });

            modelBuilder.Entity<DeckCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.DeckId, e.CardId });

                entity.ToTable("Deck_Card");

                entity.HasIndex(e => e.CardId);

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.DeckCards)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Deck_Card_Card");

                entity.HasOne(d => d.Deck)
                    .WithMany(p => p.DeckCards)
                    .HasForeignKey(d => d.DeckId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Deck_Card_Deck");
            });

            modelBuilder.Entity<DeckTagEntity>(entity =>
            {
                entity.HasKey(e => new { e.DeckId, e.TagId });

                entity.ToTable("Deck_Tag");

                entity.HasIndex(e => e.TagId);

                entity.HasOne(d => d.Deck)
                    .WithMany(p => p.DeckTags)
                    .HasForeignKey(d => d.DeckId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Deck_Tag_Deck");

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.DeckTags)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Deck_Tag_Tag");
            });

            modelBuilder.Entity<HistoryEntity>(entity =>
            {
                entity.ToTable("History");

                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.Timestamp).HasColumnType("smalldatetime");

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_History_Card");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_History_User");
            });

            modelBuilder.Entity<TagEntity>(entity =>
            {
                entity.ToTable("Tag");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(254);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(32)
                    .IsUnicode(false);
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}
