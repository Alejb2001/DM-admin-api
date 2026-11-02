using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmAdminApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase15_AoECursorDmNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGmOnly",
                table: "SceneAnnotations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GmNotes",
                table: "GameSessions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SceneTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "circle"),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    Rotation = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#f44336"),
                    SizeInCells = table.Column<double>(type: "double precision", nullable: false, defaultValue: 3.0),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneTemplates_SessionScenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "SessionScenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SceneTemplates_SceneId",
                table: "SceneTemplates",
                column: "SceneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SceneTemplates");

            migrationBuilder.DropColumn(
                name: "IsGmOnly",
                table: "SceneAnnotations");

            migrationBuilder.DropColumn(
                name: "GmNotes",
                table: "GameSessions");
        }
    }
}
