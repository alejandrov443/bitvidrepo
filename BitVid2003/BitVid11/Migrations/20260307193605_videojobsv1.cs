using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BitVid11.Migrations
{
    /// <inheritdoc />
    public partial class videojobsv1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Prompt = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    VideoPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    GalleryType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "private"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VideoJobs_UserId",
                table: "VideoJobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoJobs");
        }
    }
}
