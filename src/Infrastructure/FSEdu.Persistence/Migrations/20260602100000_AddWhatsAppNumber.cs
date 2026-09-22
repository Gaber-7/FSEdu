using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                schema: "edu",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                schema: "edu",
                table: "Users");
        }
    }
}
