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
        public DbSet<CardEffect> CardEffects { get; set; }
        public DbSet<CardSubtype> CardSubtypes { get; set; }
        public DbSet<CardCardSubtype> CardCardSubtypes { get; set; }
        public DbSet<Deck> Decks { get; set; }
        public DbSet<DeckCard> DeckCards { get; set; }
        public DbSet<Banlist> Banlists { get; set; }
        public DbSet<CardRestriction> CardRestrictions { get; set; }
        public DbSet<FavoriteCard> FavoriteCards { get; set; }
        public DbSet<CollectionCard> CollectionCards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUsers(modelBuilder);
            ConfigureCard(modelBuilder);
            ConfigureCardEffect(modelBuilder);
            ConfigureCardSubtype(modelBuilder);
            ConfigureCardCardSubtype(modelBuilder);
            ConfigureDeck(modelBuilder);
            ConfigureDeckCard(modelBuilder);
            ConfigureBanlist(modelBuilder);
            ConfigureCardRestriction(modelBuilder);
            ConfigureFavoriteCard(modelBuilder);
            ConfigureCollectionCard(modelBuilder);
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasMany(u => u.Decks)
                    .WithOne(d => d.User)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.FavoriteCards)
                    .WithOne(f => f.User)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CollectionCards)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
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

        private static void ConfigureCardEffect(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CardEffect>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .IsRequired();

                entity.Property(e => e.ActivationCondition)
                    .HasMaxLength(500);

                entity.Property(e => e.Cost)
                    .HasMaxLength(500);

                entity.Property(e => e.Resolution)
                    .HasMaxLength(1000);

                entity.HasOne(e => e.Card)
                    .WithMany(c => c.Effects)
                    .HasForeignKey(e => e.CardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.CardId)
                    .HasDatabaseName("IX_card_effects_CardId");
            });
        }

        private static void ConfigureCardSubtype(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CardSubtype>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.Category)
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();

                entity.HasIndex(s => new { s.Category, s.Name })
                    .IsUnique()
                    .HasDatabaseName("IX_card_subtypes_Category_Name");
            });
        }

        private static void ConfigureCardCardSubtype(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CardCardSubtype>(entity =>
            {
                entity.HasKey(cs => new { cs.CardId, cs.CardSubtypeId });

                entity.HasOne(cs => cs.Card)
                    .WithMany(c => c.CardSubtypes)
                    .HasForeignKey(cs => cs.CardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.CardSubtype)
                    .WithMany(s => s.Cards)
                    .HasForeignKey(cs => cs.CardSubtypeId)
                    .OnDelete(DeleteBehavior.Restrict);
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

        private static void ConfigureBanlist(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Banlist>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.Property(b => b.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(b => b.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_banlists_Name");
            });
        }

        private static void ConfigureCardRestriction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CardRestriction>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Status)
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();

                entity.HasOne(r => r.Banlist)
                    .WithMany(b => b.Restrictions)
                    .HasForeignKey(r => r.BanlistId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Card)
                    .WithMany(c => c.Restrictions)
                    .HasForeignKey(r => r.CardId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.BanlistId, r.CardId })
                    .IsUnique()
                    .HasDatabaseName("IX_card_restrictions_BanlistId_CardId");

                entity.ToTable(t =>
                    t.HasCheckConstraint("CK_card_restrictions_AllowedQuantity", "\"AllowedQuantity\" >= 0"));
            });
        }

        private static void ConfigureFavoriteCard(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FavoriteCard>(entity =>
            {
                entity.HasKey(f => new { f.UserId, f.CardId });

                entity.HasOne(f => f.Card)
                    .WithMany(c => c.FavoriteByUsers)
                    .HasForeignKey(f => f.CardId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCollectionCard(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CollectionCard>(entity =>
            {
                entity.HasKey(c => new { c.UserId, c.CardId });

                entity.HasOne(c => c.Card)
                    .WithMany(card => card.Collections)
                    .HasForeignKey(c => c.CardId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                    t.HasCheckConstraint("CK_collection_cards_Quantity", "\"Quantity\" > 0"));
            });
        }
    }
}
