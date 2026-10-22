using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Features.Courses;

//TRY TO UNDERSTAND THE CODE: This code defines the ApplicationDbContext class, which is a subclass of DbContext from Entity Framework Core. It represents the database context for the application and provides access to the Courses and CoursePrerequisites tables in the database. The OnModelCreating method is overridden to configure the relationships between the Course and CoursePrerequisite entities, including defining composite keys and foreign key constraints. Additionally, there are commented-out lines that suggest how to map database column names if they differ from the property names in the entity classes.
namespace UniversityDecisionHub.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .HasIndex(c => new { c.DepartmentId, c.CourseCode })
                .IsUnique();


            modelBuilder.Entity<CoursePrerequisite>()
                .HasKey(cp => new { cp.CourseId, cp.PrerequisiteCourseId });

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.Course)
                .WithMany(c => c.Prerequisites)
                .HasForeignKey(cp => cp.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.PrerequisiteCourse)
                .WithMany(c => c.RequiredBy)
                .HasForeignKey(cp => cp.PrerequisiteCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // If your DB column names differ (for example Coursecode), map them here to avoid migration that renames columns:
            // modelBuilder.Entity<Course>().Property(c => c.CourseCode).HasColumnName("Coursecode");
            // modelBuilder.Entity<Course>().Property(c => c.IsElective).HasColumnName("iselective");
        }
    }
}
