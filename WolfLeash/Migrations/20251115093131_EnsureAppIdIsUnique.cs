using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolfLeash.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAppIdIsUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Apps_AppId",
                table: "Apps",
                column: "AppId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apps_AppId",
                table: "Apps");
        }
    }
}
