namespace Paradigm.Server.Jobs
{
    using System;
    using Hangfire;
    using Hangfire.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Paradigm.Server.Application;

    /// <summary>
    /// Central place to register and schedule all Hangfire jobs
    /// </summary>
    public static class JobScheduler
    {
        /// <summary>
        /// Register all background jobs here
        /// Add new jobs to this method as your application grows
        /// </summary>
        public static void RegisterJobs(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IJob>>();
            logger.LogInformation("Registering Hangfire jobs...");

            try
            {
                // Clear existing recurring jobs (optional - useful during development)
                // RemoveAllRecurringJobs();

                // ========================================
                // RECURRING JOBS - Add your jobs here
                // ========================================

                // // Send email - Runs every minute
                // RecurringJob.AddOrUpdate<EmailService>(
                //     "send-test-email-job",
                //     job => job.SendTestEmail(),
                //     "*/1 * * * *", // Cron: Every 1 minute
                //     new RecurringJobOptions
                //     {
                //         TimeZone = TimeZoneInfo.Local
                //     });

                // Daily Cleanup Job - Runs at 2 AM every day
                // RecurringJob.AddOrUpdate<CleanupJob>(
                //     "daily-cleanup-job",
                //     job => job.ExecuteAsync(),
                //     Cron.Daily(2), // 2 AM daily
                //     new RecurringJobOptions
                //     {
                //         TimeZone = TimeZoneInfo.Local
                //     });

                // Weekly Report Job - Runs every Monday at 9 AM
                // RecurringJob.AddOrUpdate<WeeklyReportJob>(
                //     "weekly-report-job",
                //     job => job.ExecuteAsync(),
                //     Cron.Weekly(DayOfWeek.Monday, 9), // Monday 9 AM
                //     new RecurringJobOptions
                //     {
                //         TimeZone = TimeZoneInfo.Local
                //     });

                // ========================================
                // ONE-TIME JOBS (Fire and Forget)
                // ========================================

                // Example: Queue a one-time job
                // BackgroundJob.Enqueue<SyncNotesJob>(job => job.ExecuteAsync());

                // ========================================
                // DELAYED JOBS
                // ========================================

                // Example: Queue a job to run after 5 minutes
                // BackgroundJob.Schedule<SyncDataJob>(
                //     job => job.ExecuteAsync(),
                //     TimeSpan.FromMinutes(5));

                logger.LogInformation("All Hangfire jobs registered successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register Hangfire jobs");
                throw;
            }
        }

        /// <summary>
        /// Remove all recurring jobs (useful for cleanup)
        /// </summary>
        private static void RemoveAllRecurringJobs()
        {
            var connection = JobStorage.Current.GetConnection();
            foreach (var recurringJob in connection.GetRecurringJobs())
            {
                RecurringJob.RemoveIfExists(recurringJob.Id);
            }
        }

        /// <summary>
        /// Manually trigger a specific job (useful for testing)
        /// </summary>
        public static void TriggerJob<TJob>(IServiceProvider serviceProvider) where TJob : IJob
        {
            BackgroundJob.Enqueue<TJob>(job => job.ExecuteAsync());
        }
    }

    /// <summary>
    /// Common Cron expressions for reference
    /// </summary>
    public static class CronExpressions
    {
        // Every X minutes
        public const string Every5Minutes = "*/5 * * * *";
        public const string Every15Minutes = "*/15 * * * *";
        public const string Every30Minutes = "*/30 * * * *";

        // Hourly variations
        public const string EveryHour = "0 * * * *";
        public const string Every2Hours = "0 */2 * * *";
        public const string Every6Hours = "0 */6 * * *";

        // Daily variations
        public const string DailyAtMidnight = "0 0 * * *";
        public const string DailyAt2AM = "0 2 * * *";
        public const string DailyAt9AM = "0 9 * * *";

        // Weekly
        public const string WeeklyMonday9AM = "0 9 * * 1";
        public const string WeeklySunday = "0 0 * * 0";

        // Monthly
        public const string MonthlyFirstDay = "0 0 1 * *";
        public const string MonthlyLastDay = "0 0 L * *";
    }
}