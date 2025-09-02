using System.Threading;
using System.Threading.Tasks;

namespace KK.Pulse.Core;

/// <summary>
/// Represents job's main executable item.
/// </summary>
public interface IExecutable
{
	/// <summary>
	/// Performs execution of this item's main job .
	/// </summary>
	/// <param name="jobData"><see cref="JobData"/> context that allows accessing the job.</param>
	/// <param name="token">Token to handle cancellation of asynchronous tasks.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task ExecuteAsync(JobData jobData, CancellationToken token);
}
