using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmAdminApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_MapAndScenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionScenes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BackgroundUrl = table.Column<string>(type: "text", nullable: true),
                    GridSize = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    GridEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionScenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionScenes_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#7E57C2"),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Height = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ControlledBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapTokens_SessionScenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "SessionScenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MapTokens_Users_ControlledBy",
                        column: x => x.ControlledBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MapTokens_WorldEntities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "WorldEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapTokens_ControlledBy",
                table: "MapTokens",
                column: "ControlledBy");

            migrationBuilder.CreateIndex(
                name: "IX_MapTokens_EntityId",
                table: "MapTokens",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MapTokens_SceneId",
                table: "MapTokens",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionScenes_SessionId",
                table: "SessionScenes",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapTokens");

            migrationBuilder.DropTable(
                name: "SessionScenes");
        }
    }
}
