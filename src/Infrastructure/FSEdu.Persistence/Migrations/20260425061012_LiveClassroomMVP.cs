using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LiveClassroomMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LiveSessions_Lessons_LessonId",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "LessonId",
                schema: "edu",
                table: "LiveSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "edu",
                table: "LiveSessions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                schema: "edu",
                table: "LiveSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StageId",
                schema: "edu",
                table: "LiveSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                schema: "edu",
                table: "LiveSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "edu",
                table: "LiveSessions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_SubjectId_Status",
                schema: "edu",
                table: "LiveSessions",
                columns: new[] { "SubjectId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_LiveSessions_Lessons_LessonId",
                schema: "edu",
                table: "LiveSessions",
                column: "LessonId",
                principalSchema: "edu",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LiveSessions_Lessons_LessonId",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.DropIndex(
                name: "IX_LiveSessions_SubjectId_Status",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "StageId",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "edu",
                table: "LiveSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "LessonId",
                schema: "edu",
                table: "LiveSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LiveSessions_Lessons_LessonId",
                schema: "edu",
                table: "LiveSessions",
                column: "LessonId",
                principalSchema: "edu",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
