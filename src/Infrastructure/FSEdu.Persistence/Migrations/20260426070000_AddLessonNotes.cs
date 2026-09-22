using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonNotes",
                schema: "edu",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonNotes", x => new { x.StudentId, x.LessonId });
                    table.ForeignKey(
                        name: "FK_LessonNotes_Users_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "edu",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonNotes_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalSchema: "edu",
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonNotes_LessonId",
                schema: "edu",
                table: "LessonNotes",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonNotes_StudentId_UpdatedAtUtc",
                schema: "edu",
                table: "LessonNotes",
                columns: new[] { "StudentId", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonNotes",
                schema: "edu");
        }
    }
}
