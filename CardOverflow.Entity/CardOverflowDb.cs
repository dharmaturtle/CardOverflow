using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardOverflow.Entity
{
    public partial class CardOverflowDb : DbContext
    {

        public CardOverflowDb(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Card> Cards { get; set; }
        public virtual DbSet<Concept> Concepts { get; set; }
        public virtual DbSet<ConceptTagUser> ConceptTagUsers { get; set; }
        public virtual DbSet<Deck> Decks { get; set; }
        public virtual DbSet<DeckCard> DeckCards { get; set; }
        public virtual DbSet<DeckTag> DeckTags { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Card>(entity =>
            {
                entity.ToTable("Card");

                entity.Property(e => e.Answer)
                    .IsRequired()
                    .HasMaxLength(1028);

                entity.Property(e => e.Question)
                    .IsRequired()
                    .HasMaxLength(1028);

                entity.HasOne(d => d.Concept)
                    .WithMany(p => p.Cards)
                    .HasForeignKey(d => d.ConceptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_Concept");
            });

            modelBuilder.Entity<Concept>(entity =>
            {
                entity.ToTable("Concept");

                entity.Property(e => e.Description).HasMaxLength(512);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(128);
            });

            modelBuilder.Entity<ConceptTagUser>(entity =>
            {
                entity.HasKey(e => new { e.ConceptId, e.TagId, e.UserId });

                entity.ToTable("Concept_Tag_User");

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

            modelBuilder.Entity<Deck>(entity =>
            {
                entity.ToTable("Deck");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Decks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Deck_User");
            });

            modelBuilder.Entity<DeckCard>(entity =>
            {
                entity.HasKey(e => new { e.DeckId, e.CardId });

                entity.ToTable("Deck_Card");

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

            modelBuilder.Entity<DeckTag>(entity =>
            {
                entity.HasKey(e => new { e.DeckId, e.TagId });

                entity.ToTable("Deck_Tag");

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

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.ToTable("Tag");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<User>(entity =>
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
        }
    }
}
