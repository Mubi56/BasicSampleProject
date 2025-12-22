namespace Paradigm.Server.Jobs
{
    using System.Threading.Tasks;

    /// <summary>
    /// Base interface for all background jobs
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// Execute the job
        /// </summary>
        Task ExecuteAsync();

        /// <summary>
        /// Job name for identification
        /// </summary>
        string JobName { get; }
    }
}