using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlanAI.Models;
using System.Text.Json;

namespace PlanAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ProjectPlan> ProjectPlans { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<ProjectTask> Tasks { get; set; }
        public DbSet<Risk> Risks { get; set; }
        public DbSet<Phase> Phases { get; set; }
        public DbSet<AppUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ///////////////////////////////////////////////////////////////
            // RELATIONSHIPS
            ///////////////////////////////////////////////////////////////
            
            modelBuilder.Entity<ProjectPlan>()
                .HasOne<AppUser>()
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.UserId)
                .IsRequired(false); // Make optional for now if there are orphan plans

            modelBuilder.Entity<ProjectPlan>()
                .HasMany(p => p.Phases)
                .WithOne()
                .HasForeignKey("ProjectPlanId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Phase>()
                .HasMany(ph => ph.Tasks)
                .WithOne()
                .HasForeignKey("PhaseId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectPlan>()
                .HasMany(p => p.Risks)
                .WithOne()
                .HasForeignKey("ProjectPlanId")
                .OnDelete(DeleteBehavior.Cascade);

            ///////////////////////////////////////////////////////////////
            // JSON CONVERSION (AgentLog)
            ///////////////////////////////////////////////////////////////

            var jsonOptions = new JsonSerializerOptions();

            var listStringConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => string.IsNullOrEmpty(v)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>()
            );

            modelBuilder.Entity<ProjectPlan>()
                .Property(p => p.AgentLog)
                .HasConversion(listStringConverter);

            ///////////////////////////////////////////////////////////////
            // OWNED TYPE: ResourceSummary
            ///////////////////////////////////////////////////////////////

            modelBuilder.Entity<ProjectPlan>().OwnsOne(p => p.ResourceSummary, rs =>
            {
                rs.Property(r => r.Equipment)
                  .HasConversion(new ValueConverter<List<string>, string>(
                      v => JsonSerializer.Serialize(v, jsonOptions),
                      v => string.IsNullOrEmpty(v)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>()
                  ))
                  .IsRequired(false);
            });
        }
    }
}