using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmAdminApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_TokenRichness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuraColor",
                table: "MapTokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AuraOpacity",
                table: "MapTokens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.3);

            migrationBuilder.AddColumn<double>(
                name: "AuraRadius",
                table: "MapTokens",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bar1Color",
                table: "MapTokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bar1Max",
                table: "MapTokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bar1Value",
                table: "MapTokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bar2Color",
                table: "MapTokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bar2Max",
                table: "MapTokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bar2Value",
                table: "MapTokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "MapTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Rotation",
                table: "MapTokens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AuraColor",   table: "MapTokens");
            migrationBuilder.DropColumn(name: "AuraOpacity", table: "MapTokens");
            migrationBuilder.DropColumn(name: "AuraRadius",  table: "MapTokens");
            migrationBuilder.DropColumn(name: "Bar1Color",   table: "MapTokens");
            migrationBuilder.DropColumn(name: "Bar1Max",     table: "MapTokens");
            migrationBuilder.DropColumn(name: "Bar1Value",   table: "MapTokens");
            migrationBuilder.DropColumn(name: "Bar2Color",   table: "MapTokens");
            migrationBuilder.DropColumn(name: "Bar2Max",     table: "MapTokens");
            migrationBuilder.DropColumn(name: "Bar2Value",   table: "MapTokens");
            migrationBuilder.DropColumn(name: "IsLocked",    table: "MapTokens");
            migrationBuilder.DropColumn(name: "Rotation",    table: "MapTokens");
        }
    }
}
