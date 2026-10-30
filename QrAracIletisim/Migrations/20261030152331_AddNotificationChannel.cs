using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QrAracIletisim.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyViaWhatsApp",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyViaWhatsApp",
                table: "AspNetUsers");
        }
    }
}
