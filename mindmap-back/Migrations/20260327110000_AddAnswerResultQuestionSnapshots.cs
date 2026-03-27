using KnowledgeMap.Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mindmap_back.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260327110000_AddAnswerResultQuestionSnapshots")]
    public partial class AddAnswerResultQuestionSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnswerResultQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnswerResultId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerResultQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerResultQuestions_AnswerResults_AnswerResultId",
                        column: x => x.AnswerResultId,
                        principalTable: "AnswerResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerResultQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnswerResultQuestionId = table.Column<int>(type: "int", nullable: false),
                    AnswerOptionId = table.Column<int>(type: "int", nullable: true),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerResultQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerResultQuestionOptions_AnswerResultQuestions_AnswerResultQuestionId",
                        column: x => x.AnswerResultQuestionId,
                        principalTable: "AnswerResultQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerResultQuestionOptions_AnswerResultQuestionId_DisplayOrder",
                table: "AnswerResultQuestionOptions",
                columns: new[] { "AnswerResultQuestionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnswerResultQuestions_AnswerResultId_DisplayOrder",
                table: "AnswerResultQuestions",
                columns: new[] { "AnswerResultId", "DisplayOrder" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerResultQuestionOptions");

            migrationBuilder.DropTable(
                name: "AnswerResultQuestions");
        }
    }
}
