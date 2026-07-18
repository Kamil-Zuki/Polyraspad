using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentService.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomScenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "custom_scenario_id",
                schema: "internal",
                table: "agent_threads",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "custom_scenarios",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    target_skill = table.Column<string>(type: "text", nullable: false, defaultValue: "Speaking"),
                    system_prompt_template = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<string>(type: "text", nullable: false),
                    goals = table.Column<string>(type: "jsonb", nullable: false),
                    context_configuration = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("custom_scenarios_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_threads_custom_scenario_id",
                schema: "internal",
                table: "agent_threads",
                column: "custom_scenario_id");

            migrationBuilder.CreateIndex(
                name: "idx_custom_scenarios_user_created",
                schema: "internal",
                table: "custom_scenarios",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_agent_threads_custom_scenarios",
                schema: "internal",
                table: "agent_threads",
                column: "custom_scenario_id",
                principalSchema: "internal",
                principalTable: "custom_scenarios",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_agent_threads_custom_scenarios",
                schema: "internal",
                table: "agent_threads");

            migrationBuilder.DropTable(
                name: "custom_scenarios",
                schema: "internal");

            migrationBuilder.DropIndex(
                name: "IX_agent_threads_custom_scenario_id",
                schema: "internal",
                table: "agent_threads");

            migrationBuilder.DropColumn(
                name: "custom_scenario_id",
                schema: "internal",
                table: "agent_threads");
        }
    }
}
