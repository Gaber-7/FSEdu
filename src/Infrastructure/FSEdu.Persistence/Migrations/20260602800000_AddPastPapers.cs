using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPastPapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PastPapers",
                schema: "edu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Term = table.Column<int>(type: "int", nullable: false),
                    ExamType = table.Column<int>(type: "int", nullable: false),
                    EducationalAdministration = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalMarks = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m),
                    AnswerKeyUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastPapers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastPapers_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "edu",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PastPapers_Stages_StageId",
                        column: x => x.StageId,
                        principalSchema: "edu",
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PastPaperQuestions",
                schema: "edu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PastPaperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectAnswerJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Marks = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OrderNum = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastPaperQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastPaperQuestions_PastPapers_PastPaperId",
                        column: x => x.PastPaperId,
                        principalSchema: "edu",
                        principalTable: "PastPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PastPaperAttempts",
                schema: "edu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PastPaperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastPaperAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastPaperAttempts_PastPapers_PastPaperId",
                        column: x => x.PastPaperId,
                        principalSchema: "edu",
                        principalTable: "PastPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PastPaperAttempts_Users_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "edu",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PastPapers_StageId_SubjectId_Year_Term",
                schema: "edu",
                table: "PastPapers",
                columns: new[] { "StageId", "SubjectId", "Year", "Term" });

            migrationBuilder.CreateIndex(
                name: "IX_PastPapers_Published_Year",
                schema: "edu",
                table: "PastPapers",
                columns: new[] { "Published", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_PastPapers_SubjectId",
                schema: "edu",
                table: "PastPapers",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PastPaperQuestions_PastPaperId_OrderNum",
                schema: "edu",
                table: "PastPaperQuestions",
                columns: new[] { "PastPaperId", "OrderNum" });

            migrationBuilder.CreateIndex(
                name: "IX_PastPaperAttempts_StudentId_SubmittedAtUtc",
                schema: "edu",
                table: "PastPaperAttempts",
                columns: new[] { "StudentId", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PastPaperAttempts_PastPaperId_StudentId",
                schema: "edu",
                table: "PastPaperAttempts",
                columns: new[] { "PastPaperId", "StudentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PastPaperAttempts", schema: "edu");
            migrationBuilder.DropTable(name: "PastPaperQuestions", schema: "edu");
            migrationBuilder.DropTable(name: "PastPapers", schema: "edu");
        }
    }
}
