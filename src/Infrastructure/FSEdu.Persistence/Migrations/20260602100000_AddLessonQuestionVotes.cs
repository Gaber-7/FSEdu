using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonQuestionVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonQuestionVotes",
                schema: "edu",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonQuestionVotes", x => new { x.StudentId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_LessonQuestionVotes_Users_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "edu",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonQuestionVotes_LessonQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "edu",
                        principalTable: "LessonQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonQuestionVotes_QuestionId",
                schema: "edu",
                table: "LessonQuestionVotes",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonQuestionVotes",
                schema: "edu");
        }
    }
}
