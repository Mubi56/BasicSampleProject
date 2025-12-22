namespace Paradigm.Contract.Interface
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service for syncing data from external sources
    /// </summary>
    public interface ISyncService
    {
        /// <summary>
        /// Sync notes from external sources
        /// </summary>
        Task SyncNotesAsync();

        /// <summary>
        /// Sync general data from external APIs
        /// </summary>
        Task SyncDataAsync();
    }
}