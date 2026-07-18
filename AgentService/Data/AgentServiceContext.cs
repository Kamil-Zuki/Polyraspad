using AgentService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentService.Data;

public class AgentServiceContext : DbContext
{
    public AgentServiceContext(DbContextOptions<AgentServiceContext> options) : base(options)
    {
    }

    public virtual DbSet<AgentThread> AgentThreads { get; set; }

    public virtual DbSet<AgentMessage> AgentMessages { get; set; }

    public virtual DbSet<AgentRun> AgentRuns { get; set; }

    public virtual DbSet<AgentToolCall> AgentToolCalls { get; set; }

    public virtual DbSet<AgentDomainDecision> AgentDomainDecisions { get; set; }

    public virtual DbSet<AgentArtifact> AgentArtifacts { get; set; }

    public virtual DbSet<CustomScenario> CustomScenarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("internal");

        modelBuilder.Entity<AgentThread>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agent_threads_pkey");
            entity.ToTable("agent_threads");

            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.UpdatedAt }, "idx_agent_threads_user_project_updated")
                .IsDescending(false, false, true);

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.SystemPromptOverride).HasColumnName("system_prompt_override");
            entity.Property(e => e.CustomScenarioId).HasColumnName("custom_scenario_id");

            entity.HasOne(d => d.CustomScenario).WithMany(p => p.Threads)
                .HasForeignKey(d => d.CustomScenarioId)
                .HasConstraintName("fk_agent_threads_custom_scenarios");
        });

        modelBuilder.Entity<AgentMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agent_messages_pkey");
            entity.ToTable("agent_messages");

            entity.HasIndex(e => new { e.ThreadId, e.CreatedAt }, "idx_agent_messages_thread_created");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ThreadId).HasColumnName("thread_id");
            entity.Property(e => e.Role).HasMaxLength(16).HasColumnName("role");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasColumnName("metadata_json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.Thread).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ThreadId)
                .HasConstraintName("fk_agent_messages_threads");
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agent_runs_pkey");
            entity.ToTable("agent_runs");

            entity.HasIndex(e => e.ThreadId, "idx_agent_runs_thread_id");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.ThreadId).HasColumnName("thread_id");
            entity.Property(e => e.Status).HasMaxLength(16).HasColumnName("status");
            entity.Property(e => e.Model).HasColumnName("model");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("now()").HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Error).HasColumnName("error");

            entity.HasOne(d => d.Thread).WithMany(p => p.Runs)
                .HasForeignKey(d => d.ThreadId)
                .HasConstraintName("fk_agent_runs_threads");
        });

        modelBuilder.Entity<AgentToolCall>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agent_tool_calls_pkey");
            entity.ToTable("agent_tool_calls");

            entity.HasIndex(e => new { e.RunId, e.CreatedAt }, "idx_agent_tool_calls_run_created");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.RunId).HasColumnName("run_id");
            entity.Property(e => e.ToolName).HasColumnName("tool_name");
            entity.Property(e => e.InputJson).HasColumnType("jsonb").HasColumnName("input_json");
            entity.Property(e => e.OutputJson).HasColumnType("jsonb").HasColumnName("output_json");
            entity.Property(e => e.Status).HasMaxLength(16).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.Run).WithMany(p => p.ToolCalls)
                .HasForeignKey(d => d.RunId)
                .HasConstraintName("fk_agent_tool_calls_runs");
        });

        modelBuilder.Entity<AgentDomainDecision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agent_domain_decisions_pkey");
            entity.ToTable("agent_domain_decisions");

            entity.HasIndex(e => e.RunId, "idx_agent_domain_decisions_run_id").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.RunId).HasColumnName("run_id");
            entity.Property(e => e.Allowed).HasColumnName("allowed");
            entity.Property(e => e.Category).HasMaxLength(32).HasColumnName("category");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.UserTextPreview).HasColumnName("user_text_preview");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.Run).WithOne(p => p.DomainDecision)
                .HasForeignKey<AgentDomainDecision>(d => d.RunId)
                .HasConstraintName("fk_agent_domain_decisions_runs");
        });

        modelBuilder.Entity<AgentArtifact>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agent_artifacts_pkey");
            entity.ToTable("agent_artifacts");

            entity.HasIndex(e => new { e.ThreadId, e.CreatedAt }, "idx_agent_artifacts_thread_created");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.RunId).HasColumnName("run_id");
            entity.Property(e => e.ThreadId).HasColumnName("thread_id");
            entity.Property(e => e.Kind).HasMaxLength(32).HasColumnName("kind");
            entity.Property(e => e.PayloadJson).HasColumnType("jsonb").HasColumnName("payload_json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.Run).WithMany(p => p.Artifacts)
                .HasForeignKey(d => d.RunId)
                .HasConstraintName("fk_agent_artifacts_runs");

            entity.HasOne(d => d.Thread).WithMany(p => p.Artifacts)
                .HasForeignKey(d => d.ThreadId)
                .HasConstraintName("fk_agent_artifacts_threads");
        });

        modelBuilder.Entity<CustomScenario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("custom_scenarios_pkey");
            entity.ToTable("custom_scenarios");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_custom_scenarios_user_created");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.TargetSkill).HasDefaultValue("Speaking").HasColumnName("target_skill");
            entity.Property(e => e.SystemPromptTemplate).HasColumnName("system_prompt_template");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            
            entity.Property(e => e.Goals)
                .HasColumnType("jsonb")
                .HasColumnName("goals");
                
            entity.Property(e => e.ContextConfiguration)
                .HasColumnType("jsonb")
                .HasColumnName("context_configuration");
                
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });
    }
}
