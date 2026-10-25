using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmAdminApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase13_SceneAnnotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SceneAnnotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pen"),
                    Points = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#ef5350"),
                    StrokeWidth = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    FillColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FillOpacity = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FontSize = table.Column<int>(type: "integer", nullable: false, defaultValue: 16),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneAnnotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneAnnotations_SessionScenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "SessionScenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SceneAnnotations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SceneAnnotations_CreatedBy",
                table: "SceneAnnotations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SceneAnnotations_SceneId",
                table: "SceneAnnotations",
                column: "SceneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SceneAnnotations");
        }
    }
}
