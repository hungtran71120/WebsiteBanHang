using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HungStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatUnreadCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnreadByAdmin",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "UnreadByCustomer",
                table: "Conversations");

            migrationBuilder.AddColumn<int>(
                name: "UnreadCountForAdmin",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnreadCountForCustomer",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnreadCountForAdmin",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "UnreadCountForCustomer",
                table: "Conversations");

            migrationBuilder.AddColumn<bool>(
                name: "UnreadByAdmin",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UnreadByCustomer",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
