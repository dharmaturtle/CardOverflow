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
        public virtual DbSet<ConceptTemplateEntity> ConceptTemplates { get; set; }
        public virtual DbSet<DeckEntity> Decks { get; set; }
        public virtual DbSet<DeckCardEntity> DeckCards { get; set; }
        public virtual DbSet<HistoryEntity> Histories { get; set; }
        public virtual DbSet<PrivateTagEntity> PrivateTags { get; set; }
        public virtual DbSet<PrivateTagConceptEntity> PrivateTagConcepts { get; set; }
        public virtual DbSet<PublicTagEntity> PublicTags { get; set; }
        public virtual DbSet<PublicTagConceptEntity> PublicTagConcepts { get; set; }
        public virtual DbSet<UserEntity> Users { get; set; }

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
            modelBuilder.Entity<CardEntity>(entity =>
            {
                entity.HasIndex(e => e.CardOptionId);

                entity.HasIndex(e => e.ConceptId);

                entity.HasOne(d => d.CardOption)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.CardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_CardOption");

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_Concept");
            });

            modelBuilder.Entity<CardOptionEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

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
                entity.HasIndex(e => e.ConceptTemplateId);

                entity.HasOne(d => d.ConceptTemplate)
                    .WithMany(p => p.Concepts)
                    .HasForeignKey(d => d.ConceptTemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Concept_ConceptTemplate");
            });

            modelBuilder.Entity<ConceptTemplateEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.Css).IsUnicode(false);

                entity.HasOne(d => d.DefaultCardOption)
                    .WithMany(p => p.ConceptTemplates)
                    .HasForeignKey(d => d.DefaultCardOptionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_CardOption");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ConceptTemplates)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConceptTemplate_User");
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

            modelBuilder.Entity<PrivateTagEntity>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.PrivateTags)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_User");
            });

            modelBuilder.Entity<PrivateTagConceptEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConceptId, e.PrivateTagId });

                entity.HasIndex(e => new { e.PrivateTagId, e.ConceptId })
                    .HasName("UK_PrivateTag_Concept_PrivateTagId_ConceptId")
                    .IsUnique();

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.PrivateTagConcepts)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_Concept_Concept");

                entity.HasOne(d => d.PrivateTag)
                    .WithMany(p => p.PrivateTagConcepts)
                    .HasForeignKey(d => d.PrivateTagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PrivateTag_Concept__PrivateTag");
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

                entity.HasIndex(e => new { e.PublicTagId, e.ConceptId })
                    .HasName("UK_PublicTag_Concept_PublicTagId_ConceptId")
                    .IsUnique();

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

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}
