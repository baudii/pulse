using System.Threading;
using System.Threading.Tasks;

namespace KK.Pulse.Core;

/// <summary>
/// Main delegate for job's execution.
/// </summary>
/// <param name="jobData"><see cref="JobData"/> context that allows accessing the job.</param>
/// <param name="token">Token to handle cancellation of asynchronous tasks.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task ExecutableHandler(JobData jobData, CancellationToken token);