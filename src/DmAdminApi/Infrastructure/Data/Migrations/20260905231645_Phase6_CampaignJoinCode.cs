using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmAdminApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_CampaignJoinCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Campaigns",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValueSql: "upper(substring(md5(random()::text), 1, 8))");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_JoinCode",
                table: "Campaigns",
                column: "JoinCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Campaigns_JoinCode",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Campaigns");
        }
    }
}
