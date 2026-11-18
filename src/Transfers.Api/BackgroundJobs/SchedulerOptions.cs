namespace Transfers.Api.BackgroundJobs
{
    public sealed class SchedulerOptions
    {
        public const string SectionName = "Scheduler";

        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(10);
        public int BatchSize { get; init; } = 50;
    }
}
