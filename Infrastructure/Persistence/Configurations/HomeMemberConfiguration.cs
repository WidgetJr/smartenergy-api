using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEnergy.Api.Domain.Entities;

namespace SmartEnergy.Api.Infrastructure.Persistence.Configurations;

public class HomeMemberConfiguration : IEntityTypeConfiguration<HomeMember>
{
    public void Configure(EntityTypeBuilder<HomeMember> builder)
    {
        builder.ToTable("HomeMembers");
        builder.HasKey(member => new { member.HomeId, member.UserId });
        builder.Property(member => member.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(member => member.JoinedAt).IsRequired();
        builder.HasIndex(member => member.UserId);

        builder.HasOne(member => member.Home)
            .WithMany(home => home.HomeMembers)
            .HasForeignKey(member => member.HomeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(member => member.User)
            .WithMany(user => user.HomeMembers)
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
