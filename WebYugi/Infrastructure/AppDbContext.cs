using Microsoft.EntityFrameworkCore;
using WebYugi.Model;

namespace WebYugi.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Deck> Decks { get; set; }
        public DbSet<DeckCard> DeckCards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUsers(modelBuilder);
            ConfigureCard(modelBuilder);
            ConfigureDeck(modelBuilder);
            ConfigureDeckCard(modelBuilder);
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(u => u.Id);

                // # provavelmente serve para que apenas 1 usuario tenha acesso ao seus decks, e que ao deletar o usuario, todos os decks dele sejam deletados também
                entity.HasMany(u => u.Decks)
                    .WithOne(d => d.User)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureCard(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(c => c.Type)
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();

                entity.Property(c => c.Attribute)
                    .HasConversion<string>()
                    .HasMaxLength(16);

                entity.Property(c => c.MonsterRace)
                    .HasConversion<string>()
                    .HasMaxLength(32);

                entity.Property(c => c.Description)
                    .IsRequired();

                entity.Property(c => c.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(c => c.Archetype)
                    .HasMaxLength(100);

                entity.Property(c => c.Rarity)
                    .HasMaxLength(50);

                entity.HasIndex(c => c.Name)
                    .HasDatabaseName("IX_cards_Name");

                entity.HasIndex(c => c.Archetype)
                    .HasDatabaseName("IX_cards_Archetype");

                entity.HasIndex(c => c.Type)
                    .HasDatabaseName("IX_cards_Type");
            });
        }

        
        private static void ConfigureDeck(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Deck>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(d => d.Description)
                    .HasMaxLength(500);

                entity.HasIndex(d => d.UserId)
                    .HasDatabaseName("IX_decks_UserId");
            });
        }

        private static void ConfigureDeckCard(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeckCard>(entity =>
            {
                entity.HasKey(dc => dc.Id);

                entity.Property(dc => dc.Section)
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();

                entity.HasOne(dc => dc.Deck)
                    .WithMany(d => d.DeckCards)
                    .HasForeignKey(dc => dc.DeckId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dc => dc.Card)
                    .WithMany(c => c.DeckCards)
                    .HasForeignKey(dc => dc.CardId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(dc => new { dc.DeckId, dc.CardId, dc.Section })
                    .IsUnique()
                    .HasDatabaseName("IX_deck_cards_DeckId_CardId_Section");

                entity.ToTable(t =>
                    t.HasCheckConstraint("CK_deck_cards_Quantity", "\"Quantity\" > 0"));
            });
        }
    }
}
