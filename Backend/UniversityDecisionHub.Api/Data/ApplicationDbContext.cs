using UniversityDecisionHub.Api.Features.Reviews;
using UniversityDecisionHub.Api.Features.Users;
using UniversityDecisionHub.Api.Features.Faculties;
using UniversityDecisionHub.Api.Features.Departments;
using UniversityDecisionHub.Api.Features.Instructors;
using UniversityDecisionHub.Api.Features.Offerings;
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

        public DbSet<Faculty> Faculties => Set<Faculty>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Instructor> Instructors => Set<Instructor>();
        public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
        public DbSet<OfferingInstructor> OfferingInstructors => Set<OfferingInstructor>();

        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserAccount>().HasKey(u => u.UserId);
            modelBuilder.Entity<UserAccount>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<UserAccount>().HasOne<Department>().WithMany()
                .HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>().HasIndex(r => new { r.UserId, r.CourseId }).IsUnique();
            modelBuilder.Entity<Review>().HasOne<UserAccount>().WithMany()
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>().HasOne<Course>().WithMany()
                .HasForeignKey(r => r.CourseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>().HasOne<CourseOffering>().WithMany()
                .HasForeignKey(r => new { r.OfferingId, r.CourseId })
                .HasPrincipalKey(o => new { o.OfferingId, o.CourseId }).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>().HasOne<OfferingInstructor>().WithMany()
                .HasForeignKey(r => new { r.OfferingId, r.InstructorId }).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Faculty>().HasMany(f => f.Departments).WithOne()
                .HasForeignKey(d => d.FacultyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>().HasIndex(d => d.DepartmentCode).IsUnique();
            modelBuilder.Entity<Department>().HasMany(d => d.Instructors).WithOne()
                .HasForeignKey(i => i.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Course>().HasOne<Department>().WithMany()
                .HasForeignKey(c => c.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CourseOffering>().HasKey(o => o.OfferingId);
            modelBuilder.Entity<CourseOffering>().HasAlternateKey(o => new { o.OfferingId, o.CourseId });
            modelBuilder.Entity<CourseOffering>().HasOne<Course>().WithMany()
                .HasForeignKey(o => o.CourseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OfferingInstructor>().HasKey(a => new { a.OfferingId, a.InstructorId });
            modelBuilder.Entity<OfferingInstructor>().HasOne<CourseOffering>().WithMany()
                .HasForeignKey(a => a.OfferingId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OfferingInstructor>().HasOne<Instructor>().WithMany()
                .HasForeignKey(a => a.InstructorId).OnDelete(DeleteBehavior.Restrict);

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
