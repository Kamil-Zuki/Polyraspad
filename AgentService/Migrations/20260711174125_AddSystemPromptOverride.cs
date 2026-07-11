using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentService.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemPromptOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "system_prompt_override",
                schema: "internal",
                table: "agent_threads",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "system_prompt_override",
                schema: "internal",
                table: "agent_threads");
        }
    }
}
