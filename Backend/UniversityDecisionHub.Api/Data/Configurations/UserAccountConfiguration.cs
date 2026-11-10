using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityDecisionHub.Api.Features.Users;

namespace UniversityDecisionHub.Api.Data.Configurations
{
    public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
    {
        public const int FullNameMaxLength = 150;
        public const int EmailMaxLength = 254;

        public void Configure(EntityTypeBuilder<UserAccount> builder)
        {
            builder.ToTable("UserAccounts");
            builder.HasKey(u => u.UserId);

            builder.Property(u => u.FullName).IsRequired().HasMaxLength(FullNameMaxLength);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(EmailMaxLength);
            builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(EmailMaxLength);
            builder.Property(u => u.PasswordHash).IsRequired();

            // Case-insensitive email uniqueness is enforced by the database, not only by the controller.
            builder.HasIndex(u => u.NormalizedEmail).IsUnique();
        }
    }
}
