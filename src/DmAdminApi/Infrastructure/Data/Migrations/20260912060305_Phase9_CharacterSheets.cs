using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmAdminApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_CharacterSheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRollFormula",
                table: "EntityTypeFields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CharacterResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Current = table.Column<int>(type: "integer", nullable: false),
                    Max = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#e53935"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterResources_WorldEntities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "WorldEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterResources_EntityId",
                table: "CharacterResources",
                column: "EntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterResources");

            migrationBuilder.DropColumn(
                name: "IsRollFormula",
                table: "EntityTypeFields");
        }
    }
}
