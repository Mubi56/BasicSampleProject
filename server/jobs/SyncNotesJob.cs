namespace Paradigm.Server.Jobs
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Paradigm.Contract.Interface;

    /// <summary>
    /// Job to sync notes from external sources
    /// </summary>
    public class SyncNotesJob : IJob
    {
        private readonly ILogger<SyncNotesJob> _logger;
        private readonly ISyncService _syncService;

        public string JobName => "Sync Notes Job";

        public SyncNotesJob(
            ILogger<SyncNotesJob> logger,
            ISyncService syncService)
        {
            _logger = logger;
            _syncService = syncService;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation($"{JobName} started at {DateTime.UtcNow}");

                await _syncService.SyncNotesAsync();

                _logger.LogInformation($"{JobName} completed successfully at {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{JobName} failed: {ex.Message}");
                throw; // Re-throw to let Hangfire handle retry logic
            }
        }
    }
}