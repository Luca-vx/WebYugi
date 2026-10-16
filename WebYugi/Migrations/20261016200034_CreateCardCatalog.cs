using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebYugi.Migrations
{
    /// <inheritdoc />
    public partial class CreateCardCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "card_subtypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_subtypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Attribute = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    MonsterRace = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    Attack = table.Column<int>(type: "integer", nullable: true),
                    Defense = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Archetype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Rarity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "decks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_card_subtypes",
                columns: table => new
                {
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    CardSubtypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_card_subtypes", x => new { x.CardId, x.CardSubtypeId });
                    table.ForeignKey(
                        name: "FK_card_card_subtypes_card_subtypes_CardSubtypeId",
                        column: x => x.CardSubtypeId,
                        principalTable: "card_subtypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_card_card_subtypes_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_effects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ActivationCondition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Cost = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_effects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_card_effects_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_restrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BanlistId = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AllowedQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_restrictions", x => x.Id);
                    table.CheckConstraint("CK_card_restrictions_AllowedQuantity", "\"AllowedQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_card_restrictions_banlists_BanlistId",
                        column: x => x.BanlistId,
                        principalTable: "banlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_card_restrictions_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "collection_cards",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_cards", x => new { x.UserId, x.CardId });
                    table.CheckConstraint("CK_collection_cards_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_collection_cards_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_collection_cards_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favorite_cards",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite_cards", x => new { x.UserId, x.CardId });
                    table.ForeignKey(
                        name: "FK_favorite_cards_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_favorite_cards_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deck_cards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeckId = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Section = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deck_cards", x => x.Id);
                    table.CheckConstraint("CK_deck_cards_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_deck_cards_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_deck_cards_decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_banlists_Name",
                table: "banlists",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_card_card_subtypes_CardSubtypeId",
                table: "card_card_subtypes",
                column: "CardSubtypeId");

            migrationBuilder.CreateIndex(
                name: "IX_card_effects_CardId",
                table: "card_effects",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_card_restrictions_BanlistId_CardId",
                table: "card_restrictions",
                columns: new[] { "BanlistId", "CardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_card_restrictions_CardId",
                table: "card_restrictions",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_card_subtypes_Category_Name",
                table: "card_subtypes",
                columns: new[] { "Category", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_Archetype",
                table: "cards",
                column: "Archetype");

            migrationBuilder.CreateIndex(
                name: "IX_cards_Name",
                table: "cards",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_cards_Type",
                table: "cards",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_collection_cards_CardId",
                table: "collection_cards",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_deck_cards_CardId",
                table: "deck_cards",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_deck_cards_DeckId_CardId_Section",
                table: "deck_cards",
                columns: new[] { "DeckId", "CardId", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_decks_UserId",
                table: "decks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_favorite_cards_CardId",
                table: "favorite_cards",
                column: "CardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_card_subtypes");

            migrationBuilder.DropTable(
                name: "card_effects");

            migrationBuilder.DropTable(
                name: "card_restrictions");

            migrationBuilder.DropTable(
                name: "collection_cards");

            migrationBuilder.DropTable(
                name: "deck_cards");

            migrationBuilder.DropTable(
                name: "favorite_cards");

            migrationBuilder.DropTable(
                name: "card_subtypes");

            migrationBuilder.DropTable(
                name: "banlists");

            migrationBuilder.DropTable(
                name: "decks");

            migrationBuilder.DropTable(
                name: "cards");
        }
    }
}
