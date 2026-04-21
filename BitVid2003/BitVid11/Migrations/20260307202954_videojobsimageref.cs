using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitVid11.Migrations
{
    /// <inheritdoc />
    public partial class videojobsimageref : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "uploadedImagePath",
                table: "VideoJobs",
                type: "longtext",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uploadedImagePath",
                table: "VideoJobs");
        }
    }
}
