using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityDecisionHub.Api.Features.Programmes;
using UniversityDecisionHub.Api.Features.Departments;
using UniversityDecisionHub.Api.Features.Courses;

namespace UniversityDecisionHub.Api.Data.Configurations;

public class ProgrammeConfiguration : IEntityTypeConfiguration<Programme>
{
    public void Configure(EntityTypeBuilder<Programme> b)
    {
        b.HasKey(p => p.ProgrammeId);
        b.Property(p => p.ProgrammeKey).HasMaxLength(100).IsRequired();
        b.HasIndex(p => p.ProgrammeKey).IsUnique();
        b.Property(p => p.ProgrammeName).HasMaxLength(200).IsRequired();
        b.Property(p => p.DegreeLevel).HasMaxLength(100);
        b.Property(p => p.InstructionLanguage).HasMaxLength(100);
        b.Property(p => p.BolognaProgrammeId).HasMaxLength(100);
        b.HasOne<Department>().WithMany().HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProgrammeCourseConfiguration : IEntityTypeConfiguration<ProgrammeCourse>
{
    public void Configure(EntityTypeBuilder<ProgrammeCourse> b)
    {
        b.HasKey(p => p.ProgrammeCourseId);
        b.Property(p => p.CurriculumKey).HasMaxLength(100);
        b.HasIndex(p => p.CurriculumKey).IsUnique();
        b.Property(p => p.CatalogueYear).HasMaxLength(50);
        b.HasOne<Programme>().WithMany().HasForeignKey(p => p.ProgrammeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Course>().WithMany().HasForeignKey(p => p.CourseId).OnDelete(DeleteBehavior.Restrict);
        // Not unique: the same course may occur in multiple curricula/semesters.
        b.HasIndex(p => new { p.ProgrammeId, p.CourseId });
        b.ToTable("ProgrammeCourses", t =>
        {
            t.HasCheckConstraint("CK_ProgrammeCourses_Year", "\"RecommendedStudyYear\" IS NULL OR \"RecommendedStudyYear\" > 0");
            t.HasCheckConstraint("CK_ProgrammeCourses_Semester", "\"Semester\" IS NULL OR \"Semester\" > 0");
            t.HasCheckConstraint("CK_ProgrammeCourses_Credit", "\"CourseCredit\" IS NULL OR \"CourseCredit\" >= 0");
            t.HasCheckConstraint("CK_ProgrammeCourses_Ects", "\"Ects\" IS NULL OR \"Ects\" >= 0");
        });
    }
}
