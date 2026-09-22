using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManualPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderTxnId",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Provider",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderTxnId",
                schema: "edu",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "Method",
                schema: "edu",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "edu",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptImageUrl",
                schema: "edu",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                schema: "edu",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                schema: "edu",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                schema: "edu",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedBy",
                schema: "edu",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                schema: "edu",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                schema: "edu",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "edu",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_SubmittedAtUtc",
                schema: "edu",
                table: "Payments",
                columns: new[] { "Status", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                schema: "edu",
                table: "Payments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_SubmittedAtUtc",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UserId",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Method",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReceiptImageUrl",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SenderName",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                schema: "edu",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "edu",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                schema: "edu",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTxnId",
                schema: "edu",
                table: "Payments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderTxnId",
                schema: "edu",
                table: "Payments",
                column: "ProviderTxnId");
        }
    }
}
