using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReferrals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Student-specific columns on the TPH Users table.
            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                schema: "edu",
                table: "Users",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferredByCode",
                schema: "edu",
                table: "Users",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferralCredits",
                schema: "edu",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReferralCode",
                schema: "edu",
                table: "Users",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReferredByCode",
                schema: "edu",
                table: "Users",
                column: "ReferredByCode",
                filter: "[ReferredByCode] IS NOT NULL");

            migrationBuilder.CreateTable(
                name: "Referrals",
                schema: "edu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferrerStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferredStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RewardedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RewardEgp = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TriggerSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferrerStudentId",
                        column: x => x.ReferrerStudentId,
                        principalSchema: "edu",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferredStudentId",
                        column: x => x.ReferredStudentId,
                        principalSchema: "edu",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredStudentId",
                schema: "edu",
                table: "Referrals",
                column: "ReferredStudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerStudentId",
                schema: "edu",
                table: "Referrals",
                column: "ReferrerStudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Referrals", schema: "edu");
            migrationBuilder.DropIndex(name: "IX_Users_ReferralCode", schema: "edu", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_ReferredByCode", schema: "edu", table: "Users");
            migrationBuilder.DropColumn(name: "ReferralCode", schema: "edu", table: "Users");
            migrationBuilder.DropColumn(name: "ReferredByCode", schema: "edu", table: "Users");
            migrationBuilder.DropColumn(name: "ReferralCredits", schema: "edu", table: "Users");
        }
    }
}
