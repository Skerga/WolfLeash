using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolfLeash.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Runners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Image = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Mounts = table.Column<string>(type: "TEXT", nullable: false),
                    Env = table.Column<string>(type: "TEXT", nullable: false),
                    Devices = table.Column<string>(type: "TEXT", nullable: false),
                    Ports = table.Column<string>(type: "TEXT", nullable: false),
                    BaseCreateJson = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Apps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IconPngPath = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SupportHdr = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartVirtualCompositor = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartAudioServer = table.Column<bool>(type: "INTEGER", nullable: false),
                    RunnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    RenderNode = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    H264_gst_pipeline = table.Column<string>(type: "TEXT", nullable: false),
                    Hevc_gst_pipeline = table.Column<string>(type: "TEXT", nullable: false),
                    Av1_gst_pipeline = table.Column<string>(type: "TEXT", nullable: false),
                    Opus_gst_pipeline = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Apps_Runners_RunnerId",
                        column: x => x.RunnerId,
                        principalTable: "Runners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Apps_RunnerId",
                table: "Apps",
                column: "RunnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apps");

            migrationBuilder.DropTable(
                name: "Runners");
        }
    }
}
