using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebYugi.Migrations
{
    /// <inheritdoc />
    public partial class HardenAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PasswordVersion",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SenhaHashed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("UPDATE users SET \"NormalizedEmail\" = lower(trim(\"Email\")), \"NormalizedName\" = upper(trim(\"Name\"));");
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM users WHERE length(\"NormalizedEmail\") BETWEEN 3 AND 254 GROUP BY \"NormalizedEmail\" HAVING count(*) > 1) OR EXISTS (SELECT 1 FROM users WHERE length(\"NormalizedName\") BETWEEN 3 AND 100 GROUP BY \"NormalizedName\" HAVING count(*) > 1) THEN RAISE EXCEPTION 'Authentication migration stopped: duplicate normalized names or emails within current signup limits must be resolved before applying.'; END IF; END $$;");
            migrationBuilder.AlterColumn<string>(name: "NormalizedEmail", table: "users", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "NormalizedName", table: "users", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedEmail",
                table: "users",
                column: "NormalizedEmail",
                unique: true,
                filter: "length(\"NormalizedEmail\") BETWEEN 3 AND 254");

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedName",
                table: "users",
                column: "NormalizedName",
                unique: true,
                filter: "length(\"NormalizedName\") BETWEEN 3 AND 100");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_TokenHash",
                table: "password_reset_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId_ExpiresAt",
                table: "password_reset_tokens",
                columns: new[] { "UserId", "ExpiresAt" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_users_NormalizedEmail",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_NormalizedName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordVersion",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SenhaHashed",
                table: "users");
        }
    }
}
