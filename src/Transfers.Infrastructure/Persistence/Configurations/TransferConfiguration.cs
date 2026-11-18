using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Configurations
{
    public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
    {
        public void Configure(EntityTypeBuilder<Transfer> builder)
        {
            builder.ToTable("Transfers");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();
            builder.Property(t => t.SourceAccountId).IsRequired();
            builder.Property(t => t.DestinationAccountId).IsRequired();
            builder.Property(t => t.Amount).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.ScheduledFor).IsRequired(false);
            builder.Property(t => t.ProcessedAt).IsRequired(false);
            builder.Property(t => t.FailureReason).HasConversion<string>().HasMaxLength(30).IsRequired(false);

            builder.HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => new { t.SourceAccountId, t.ProcessedAt });

            builder.HasIndex(t => new { t.Status, t.ScheduledFor });
        }
    }
}
