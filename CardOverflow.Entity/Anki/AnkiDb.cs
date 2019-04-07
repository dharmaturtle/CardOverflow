using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardOverflow.Entity.Anki
{
    public partial class AnkiDb : DbContext
    {
        public virtual DbSet<CardEntity> Cards { get; set; }
        public virtual DbSet<ColEntity> Cols { get; set; }
        public virtual DbSet<NoteEntity> Notes { get; set; }
        public virtual DbSet<RevlogEntity> Revlogs { get; set; }
        // Unable to generate entity type for table 'graves'. Please see the warning messages.


        public AnkiDb(DbContextOptions options) : base(options)
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
                entity.HasIndex(e => e.Nid)
                    .HasName("ix_cards_nid");

                entity.HasIndex(e => e.Usn)
                    .HasName("ix_cards_usn");

                entity.HasIndex(e => new { e.Did, e.Queue, e.Due })
                    .HasName("ix_cards_sched");

                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<ColEntity>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<NoteEntity>(entity =>
            {
                entity.HasIndex(e => e.Csum)
                    .HasName("ix_notes_csum");

                entity.HasIndex(e => e.Usn)
                    .HasName("ix_notes_usn");

                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<RevlogEntity>(entity =>
            {
                entity.HasIndex(e => e.Cid)
                    .HasName("ix_revlog_cid");

                entity.HasIndex(e => e.Usn)
                    .HasName("ix_revlog_usn");

                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            OnModelCreatingExt(modelBuilder);
        }

        partial void OnModelCreatingExt(ModelBuilder modelBuilder);
    }
}
