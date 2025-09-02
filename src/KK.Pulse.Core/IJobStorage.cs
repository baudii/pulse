using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KK.Pulse.Core;

/// <summary>
/// Provides methods that allows CRUD operations on <see cref="JobData"/>.
/// </summary>
public interface IJobStorage
{
	/// <summary>
	/// Attempts to get a job from the pool using id.
	/// </summary>
	/// <param name="id">Id assigned to the job.</param>
	/// <param name="jobData">When this method returns, contains the instance of <see cref="JobData"/> if the job was found; otherwise null.</param>
	/// <returns><see langword="true"/> if the job was found; otherwise <see langword="false"/></returns>
	Task<JobData?> GetJobAsync(string id, CancellationToken token);

	/// <summary>
	/// Attempts to remove a job from the pool.
	/// </summary>
	/// <param name="id">Id of the job.</param>
	/// <param name="jobData">When this method returns, contains the instance of <see cref="JobData"/> if the job was removed; otherwise null.</param>
	/// <returns><see langword="true"/> if removed successfully; otherwise <see langword="false"/></returns>
	Task<JobData?> RemoveJobAsync(string id, CancellationToken token);

	/// <summary>
	/// Attempts to update a job.
	/// </summary>
	/// <param name="jobData"><see cref="JobData"/> instance to update.</param>
	/// <returns><see langword="true"/> if removed successfully; otherwise <see langword="false"/></returns>
	Task<bool> TryUpdateJobAsync(JobData jobData, CancellationToken token);

	/// <summary>
	/// Attempts to add a job to the pool.
	/// </summary>
	/// <param name="jobData"><see cref="JobData"/> instance to add.</param>
	/// <returns><see langword="true"/> if removed successfully; otherwise <see langword="false"/></returns>
	Task<bool> TryAddJobAsync(JobData jobData, CancellationToken token);

	/// <summary>
	/// Retrieves an enumeration of all existing jobs.
	/// </summary>
	/// <returns>Enumeration of existing jobs.</returns>
	IAsyncEnumerable<JobData> EnumerateJobsAsync(CancellationToken token);
}
