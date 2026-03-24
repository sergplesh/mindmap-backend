using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace mindmap_back.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Emoji = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maps_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accesses", x => x.Id);
                    table.CheckConstraint("CK_Access_Role", "[Role] IN ('observer', 'learner')");
                    table.ForeignKey(
                        name: "FK_Accesses_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EdgeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Style = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdgeTypes", x => x.Id);
                    table.CheckConstraint("CK_EdgeType_Scope", "(([MapId] IS NULL AND [IsSystem] = 1) OR ([MapId] IS NOT NULL AND [IsSystem] = 0))");
                    table.ForeignKey(
                        name: "FK_EdgeTypes_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Shape = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Size = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeTypes", x => x.Id);
                    table.CheckConstraint("CK_NodeType_Scope", "(([MapId] IS NULL AND [IsSystem] = 1) OR ([MapId] IS NOT NULL AND [IsSystem] = 0))");
                    table.ForeignKey(
                        name: "FK_NodeTypes_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapId = table.Column<int>(type: "int", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XPosition = table.Column<double>(type: "float", nullable: false),
                    YPosition = table.Column<double>(type: "float", nullable: false),
                    Width = table.Column<double>(type: "float", nullable: false),
                    Height = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiresQuiz = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nodes_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_NodeTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "NodeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NodeTypeFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Validation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeTypeFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeTypeFieldDefinitions_NodeTypes_NodeTypeId",
                        column: x => x.NodeTypeId,
                        principalTable: "NodeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerResults_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswerResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Edges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceNodeId = table.Column<int>(type: "int", nullable: false),
                    TargetNodeId = table.Column<int>(type: "int", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: true),
                    IsHierarchy = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Edges", x => x.Id);
                    table.CheckConstraint("CK_Edge_NoSelfReference", "[SourceNodeId] <> [TargetNodeId]");
                    table.ForeignKey(
                        name: "FK_Edges_EdgeTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "EdgeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Edges_Nodes_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Edges_Nodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    MasteryLevel = table.Column<int>(type: "int", nullable: false),
                    PersonalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningProgresses_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.CheckConstraint("CK_Question_QuestionType", "[QuestionType] IN ('single_choice', 'multiple_choice')");
                    table.ForeignKey(
                        name: "FK_Questions_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeFieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    NodeTypeFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeFieldValues_NodeTypeFieldDefinitions_NodeTypeFieldDefinitionId",
                        column: x => x.NodeTypeFieldDefinitionId,
                        principalTable: "NodeTypeFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NodeFieldValues_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeTypeFieldOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeTypeFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeTypeFieldOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeTypeFieldOptions_NodeTypeFieldDefinitions_NodeTypeFieldDefinitionId",
                        column: x => x.NodeTypeFieldDefinitionId,
                        principalTable: "NodeTypeFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerResultSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnswerResultId = table.Column<int>(type: "int", nullable: false),
                    AnswerOptionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerResultSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerResultSelections_AnswerOptions_AnswerOptionId",
                        column: x => x.AnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnswerResultSelections_AnswerResults_AnswerResultId",
                        column: x => x.AnswerResultId,
                        principalTable: "AnswerResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EdgeTypes",
                columns: new[] { "Id", "Color", "IsSystem", "Label", "MapId", "Name", "Style" },
                values: new object[,]
                {
                    { 1, "#666666", true, "является", null, "is_a", "solid" },
                    { 2, "#666666", true, "использует", null, "uses", "dashed" },
                    { 3, "#666666", true, "требует", null, "requires", "solid" },
                    { 4, "#666666", true, "отличие", null, "contrasts", "dotted" },
                    { 5, "#666666", true, "доказывает", null, "proves", "dashed" }
                });

            migrationBuilder.InsertData(
                table: "NodeTypes",
                columns: new[] { "Id", "Color", "Icon", "IsSystem", "MapId", "Name", "Shape", "Size" },
                values: new object[,]
                {
                    { 1, "#3b82f6", "psychology", true, null, "Понятие", "rect", "medium" },
                    { 2, "#10b981", "description", true, null, "Определение", "rect", "medium" },
                    { 3, "#ef4444", "route", true, null, "Алгоритм", "rect", "medium" },
                    { 4, "#f59e0b", "star", true, null, "Свойство", "rect", "medium" },
                    { 5, "#8b5cf6", "calculate", true, null, "Теорема", "rect", "medium" },
                    { 6, "#6b7280", "code", true, null, "Пример", "rect", "medium" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accesses_MapId_UserId",
                table: "Accesses",
                columns: new[] { "MapId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accesses_UserId",
                table: "Accesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId",
                table: "AnswerOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerResults_NodeId",
                table: "AnswerResults",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerResults_UserId_NodeId_CompletedAt",
                table: "AnswerResults",
                columns: new[] { "UserId", "NodeId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerResultSelections_AnswerOptionId",
                table: "AnswerResultSelections",
                column: "AnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerResultSelections_AnswerResultId_AnswerOptionId",
                table: "AnswerResultSelections",
                columns: new[] { "AnswerResultId", "AnswerOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Edges_SourceNodeId_TargetNodeId",
                table: "Edges",
                columns: new[] { "SourceNodeId", "TargetNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Edges_TargetNodeId",
                table: "Edges",
                column: "TargetNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_TypeId",
                table: "Edges",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeTypes_MapId_Name",
                table: "EdgeTypes",
                columns: new[] { "MapId", "Name" },
                unique: true,
                filter: "[MapId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeTypes_Name",
                table: "EdgeTypes",
                column: "Name",
                unique: true,
                filter: "[MapId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LearningProgresses_NodeId",
                table: "LearningProgresses",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningProgresses_UserId_NodeId",
                table: "LearningProgresses",
                columns: new[] { "UserId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Maps_OwnerId",
                table: "Maps",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeFieldValues_NodeId_NodeTypeFieldDefinitionId",
                table: "NodeFieldValues",
                columns: new[] { "NodeId", "NodeTypeFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeFieldValues_NodeTypeFieldDefinitionId",
                table: "NodeFieldValues",
                column: "NodeTypeFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_MapId",
                table: "Nodes",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_TypeId",
                table: "Nodes",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeTypeFieldDefinitions_NodeTypeId_Name",
                table: "NodeTypeFieldDefinitions",
                columns: new[] { "NodeTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeTypeFieldOptions_NodeTypeFieldDefinitionId_SortOrder",
                table: "NodeTypeFieldOptions",
                columns: new[] { "NodeTypeFieldDefinitionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeTypes_MapId_Name",
                table: "NodeTypes",
                columns: new[] { "MapId", "Name" },
                unique: true,
                filter: "[MapId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NodeTypes_Name",
                table: "NodeTypes",
                column: "Name",
                unique: true,
                filter: "[MapId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_NodeId",
                table: "Questions",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accesses");

            migrationBuilder.DropTable(
                name: "AnswerResultSelections");

            migrationBuilder.DropTable(
                name: "Edges");

            migrationBuilder.DropTable(
                name: "LearningProgresses");

            migrationBuilder.DropTable(
                name: "NodeFieldValues");

            migrationBuilder.DropTable(
                name: "NodeTypeFieldOptions");

            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "AnswerResults");

            migrationBuilder.DropTable(
                name: "EdgeTypes");

            migrationBuilder.DropTable(
                name: "NodeTypeFieldDefinitions");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "NodeTypes");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
