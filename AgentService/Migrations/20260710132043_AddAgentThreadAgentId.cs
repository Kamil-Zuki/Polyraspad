using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentService.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentThreadAgentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agent_id",
                schema: "internal",
                table: "agent_threads",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE internal.agent_threads
                SET agent_id = 'card-janitor'
                WHERE title LIKE '[card-janitor] %';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agent_id",
                schema: "internal",
                table: "agent_threads");
        }
    }
}
