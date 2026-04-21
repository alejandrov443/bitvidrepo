using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitVid11.Migrations
{
    /// <inheritdoc />
    public partial class addvideourl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "ChatMessages",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "ChatMessages");
        }
    }
}
