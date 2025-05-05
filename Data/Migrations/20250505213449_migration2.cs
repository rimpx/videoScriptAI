using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace videoscriptAI.Data.Migrations
{
    public partial class migration2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaPath",
                table: "ChatContents");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "Chats",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "SentAt",
                table: "ChatContents",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "ChatContents",
                newName: "Content");

            migrationBuilder.AddColumn<bool>(
                name: "IsFromUser",
                table: "ChatContents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFromUser",
                table: "ChatContents");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Chats",
                newName: "ContentType");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ChatContents",
                newName: "SentAt");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "ChatContents",
                newName: "Message");

            migrationBuilder.AddColumn<string>(
                name: "MediaPath",
                table: "ChatContents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
