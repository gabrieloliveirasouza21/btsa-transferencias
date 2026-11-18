using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        // Fixed ids: seed data must be deterministic, otherwise every migration would see a "change".
        public static readonly Guid AnaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid BrunoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid CarlaId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DiegoId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedNever();
            builder.Property(a => a.HolderName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Balance).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(a => a.OverdraftLimit).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Ignore(a => a.AvailableAmount);

            builder.OwnsOne(a => a.Limits, limits =>
            {
                limits.Property(l => l.DayMaxAmountPerHour).HasColumnName("DayMaxAmountPerHour").IsRequired().HasColumnType("decimal(18,2)");
                limits.Property(l => l.DayMaxAttemptsPerHour).HasColumnName("DayMaxAttemptsPerHour").IsRequired();
                limits.Property(l => l.NightMaxAmountPerHour).HasColumnName("NightMaxAmountPerHour").IsRequired().HasColumnType("decimal(18,2)");
                limits.Property(l => l.NightMaxAttemptsPerHour).HasColumnName("NightMaxAttemptsPerHour").IsRequired();

                // Owned seed: same row, but EF treats it as a separate entity keyed by the shadow property AccountId.
                limits.HasData(
                    PdfLimits(AnaId),
                    PdfLimits(BrunoId),
                    PdfLimits(CarlaId),
                    PdfLimits(DiegoId));
            });

            // Owner seed: anonymous objects, without the Limits navigation.
            builder.HasData(
                new { Id = AnaId, HolderName = "Ana Souza", Balance = 500m, OverdraftLimit = 1000m, Status = AccountStatus.Active },
                new { Id = BrunoId, HolderName = "Bruno Lima", Balance = -800m, OverdraftLimit = 1000m, Status = AccountStatus.Active },
                new { Id = CarlaId, HolderName = "Carla Mendes", Balance = 10000m, OverdraftLimit = 1000m, Status = AccountStatus.Active },
                new { Id = DiegoId, HolderName = "Diego Rocha", Balance = 0m, OverdraftLimit = 0m, Status = AccountStatus.Blocked });
        }

        // Limits from the spec: day 5000 / 5 attempts per hour, night 1000 / 3 attempts per hour.
        private static object PdfLimits(Guid accountId) => new
        {
            AccountId = accountId,
            DayMaxAmountPerHour = 5000m,
            DayMaxAttemptsPerHour = 5,
            NightMaxAmountPerHour = 1000m,
            NightMaxAttemptsPerHour = 3
        };
    }
}
