using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KK.Pulse.Core;

/// <summary>
/// Represents an asynchronous content generation job with life cycle management.
/// </summary>
internal sealed class JobWorker(ExecutableHandler handler, TimeSpan maxRunTime) : IDisposable
{
	/// <summary>
	/// Gets the result of successful job execution, otherwise null.
	/// </summary>
	public ExecutableHandler Handler => handler;

	private readonly CancellationTokenSource _localCts = new();

	/// <summary>
	/// Runs asynchronously this job's executable.
	/// </summary>
	/// <param name="jobData"><see cref="JobData"/> connected to this worker.</param>
	/// <param name="token">Token to handle cancellation of asynchronous tasks.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task ExecuteAsync(JobData jobData, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		using var timeoutCts = new CancellationTokenSource(maxRunTime);
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localCts.Token, token, timeoutCts.Token);
		var stopwatch = Stopwatch.StartNew();
		try
		{
			await Handler.Invoke(jobData, linkedCts.Token).ConfigureAwait(false);
		}
		finally
		{
			stopwatch.Stop();
			jobData.ExecutionTime = stopwatch.Elapsed.Ticks;
		}
	}

	/// <summary>
	/// Cancels this worker.
	/// </summary>
	public void Cancel()
	{
		_localCts.Cancel();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_localCts.Cancel();
		_localCts.Dispose();
	}
}