using KnowledgeMap.Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mindmap_back.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324080000_AddEdgeLabel")]
    public partial class AddEdgeLabel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "Edges",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Label",
                table: "Edges");
        }
    }
}
