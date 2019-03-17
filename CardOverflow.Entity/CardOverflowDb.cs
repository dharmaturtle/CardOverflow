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
        public virtual DbSet<CardDeck> CardDecks { get; set; }
        public virtual DbSet<Deck> Decks { get; set; }
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
                    .HasMaxLength(1000);

                entity.Property(e => e.Question)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<CardDeck>(entity =>
            {
                entity.HasKey(e => new { e.CardId, e.DeckId });

                entity.ToTable("CardDeck");

                entity.HasOne(d => d.Card)
                    .WithMany(p => p.CardDecks)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CardDeck_Card");

                entity.HasOne(d => d.Deck)
                    .WithMany(p => p.CardDecks)
                    .HasForeignKey(d => d.DeckId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CardDeck_Deck");
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
