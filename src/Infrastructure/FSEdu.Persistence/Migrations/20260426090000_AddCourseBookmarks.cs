using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseBookmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseBookmarks",
                schema: "edu",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseBookmarks", x => new { x.StudentId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_CourseBookmarks_Users_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "edu",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseBookmarks_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "edu",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseBookmarks_CourseId",
                schema: "edu",
                table: "CourseBookmarks",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseBookmarks_StudentId_CreatedAtUtc",
                schema: "edu",
                table: "CourseBookmarks",
                columns: new[] { "StudentId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseBookmarks",
                schema: "edu");
        }
    }
}
