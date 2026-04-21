using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitVid11.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageUidToChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageUid",
                table: "ChatMessages",
                type: "longtext",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageUid",
                table: "ChatMessages");
        }
    }
}
