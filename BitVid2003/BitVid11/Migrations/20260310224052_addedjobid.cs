using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitVid11.Migrations
{
    /// <inheritdoc />
    public partial class addedjobid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobIdentification",
                table: "VideoJobs",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobIdentification",
                table: "VideoJobs");
        }
    }
}
