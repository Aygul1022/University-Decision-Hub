using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityDecisionHub.Api.Features.Instructors;

namespace UniversityDecisionHub.Api.Data.Configurations
{
    public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
    {
        public const int FullNameMaxLength = 150;
        public const int AcademicTitleMaxLength = 50;

        public void Configure(EntityTypeBuilder<Instructor> builder)
        {
            builder.ToTable("Instructors");
            builder.HasKey(i => i.InstructorId);

            builder.Property(i => i.FullName).IsRequired().HasMaxLength(FullNameMaxLength);
            builder.Property(i => i.AcademicTitle).HasMaxLength(AcademicTitleMaxLength);

            // Names are not unique: two people can share one. The list is usually filtered by department.
            builder.HasIndex(i => i.DepartmentId);
        }
    }
}
