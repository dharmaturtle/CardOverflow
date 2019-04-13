using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {
        public virtual DbSet<CardEntity> Cards { get; set; }
        public virtual DbSet<ConceptEntity> Concepts { get; set; }
        public virtual DbSet<ConceptOptionEntity> ConceptOptions { get; set; }
        public virtual DbSet<ConceptTagUserEntity> ConceptTagUsers { get; set; }
        public virtual DbSet<ConceptTemplateEntity> ConceptTemplates { get; set; }
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
                entity.HasIndex(e => e.ConceptId);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_Concept");
            });

            modelBuilder.Entity<ConceptEntity>(entity =>
            {
                entity.HasOne(d => d.ConceptOption)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.ConceptOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_ConceptOption");

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_ConceptTemplate");
            });

            modelBuilder.Entity<ConceptOptionEntity>(entity =>
            {
                entity.Property(e => e.LapsedCardsStepsInMinutes).IsUnicode(false);

                entity.Property(e => e.NewCardsStepsInMinutes).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ConceptOptions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptOption_User");
            });

            modelBuilder.Entity<ConceptTagUserEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptId, e.TagId, e.UserId });

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

            modelBuilder.Entity<ConceptTemplateEntity>(entity =>
            {
                entity.Property(e => e.Css).IsUnicode(false);

                entity.HasOne(d => d.DefaultConceptOptions)
                    .WithMany(p => p.ConceptTemplates)
                    .HasForeignKey(d => d.DefaultConceptOptionsId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_ConceptOption");
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

            modelBuilder.Entity<DeckCardEntity>(entity =>
            {
                entity.HasKey(e => new { e.DeckId, e.CardId });

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
                entity.HasIndex(e => e.CardId);

                entity.HasIndex(e => e.UserId);

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

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.Property(e => e.Name).IsUnicode(false);
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}
