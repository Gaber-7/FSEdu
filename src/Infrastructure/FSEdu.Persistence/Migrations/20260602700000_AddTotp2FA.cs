using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTotp2FA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TotpSecret",
                schema: "edu",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TotpEnabled",
                schema: "edu",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TotpEnabledAtUtc",
                schema: "edu",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotpRecoveryCodesHashed",
                schema: "edu",
                table: "Users",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TotpSecret", schema: "edu", table: "Users");
            migrationBuilder.DropColumn(name: "TotpEnabled", schema: "edu", table: "Users");
            migrationBuilder.DropColumn(name: "TotpEnabledAtUtc", schema: "edu", table: "Users");
            migrationBuilder.DropColumn(name: "TotpRecoveryCodesHashed", schema: "edu", table: "Users");
        }
    }
}
