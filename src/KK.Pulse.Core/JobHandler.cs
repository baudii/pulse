using KK.Pulse.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KK.Pulse.Core;

/// <summary>
/// Manages jobs' storage, execution and error handling.
/// </summary>
public sealed class JobHandler : IDisposable
{
	private readonly IJobStorage _jobStorage;

	private readonly ILogger<JobHandler> _logger;

	private readonly PulseConfig _jobConfiguration;

	private readonly SemaphoreSlim _semaphoreParallelismLimiter;

	private readonly SemaphoreSlim _semaphoreQueueLimiter;

	private readonly ConcurrentDictionary<string, JobWorker> _activeWorkers;

	/// <summary>
	/// Initializes a new instance of the <see cref="JobHandler"/> class.
	/// </summary>
	/// <param name="jobConfiguration">Configuration instance with rules for jobs behavior.</param>
	/// <param name="logger">Logger instance for tracking job execution.</param>
	public JobHandler(IJobStorage jobStorage, ILogger<JobHandler> logger, IOptions<PulseConfig> jobConfiguration)
	{
		_jobConfiguration = jobConfiguration.Value;
		_jobStorage = jobStorage;
		_activeWorkers = new ConcurrentDictionary<string, JobWorker>();
		_semaphoreParallelismLimiter = new SemaphoreSlim(_jobConfiguration.MaxDegreeParallelism, _jobConfiguration.MaxDegreeParallelism);
		_semaphoreQueueLimiter = new SemaphoreSlim(_jobConfiguration.MaxJobQueueSize, _jobConfiguration.MaxJobQueueSize);
		_logger = logger;
	}

	/// <summary>
	/// Schedules job asynchronously in the channel and saves it with unique <see cref="Guid"/>.
	/// </summary>
	/// <param name="executableItem">Executable item containing job's working methods.</param>
	/// <param name="token">Token to handle cancellation of asynchronous tasks.</param>
	/// <returns>Newly created job.</returns>
	public Task<JobData> ScheduleJobAsync(IExecutable executableItem, CancellationToken token)
	{
		string id = Guid.NewGuid().ToString("N");

		return CreateJobAsync(id, executableItem.ExecuteAsync, token);
	}

	/// <summary>
	/// Schedules job asynchronously in the channel and saves it with unique <see cref="Guid"/>.
	/// </summary>
	/// <param name="executableItem">Executable item containing job's working methods.</param>
	/// <param name="token">Token to handle cancellation of asynchronous tasks.</param>
	/// <returns>Newly created job.</returns>
	public Task<JobData> ScheduleJobAsync(ExecutableHandler executionHandler, CancellationToken token)
	{
		string id = Guid.NewGuid().ToString("N");

		return CreateJobAsync(id, executionHandler, token);
	}

	public Task<JobData> ScheduleJobAsync(string id, IExecutable executableItem, bool overwrite, CancellationToken token)
	{
		return ScheduleJobAsync(id, executableItem.ExecuteAsync, overwrite, token);
	}

	/// <summary>
	/// Schedules job in channel asynchronously with given id and executable.
	/// </summary>
	/// <param name="id">Id that will be assigned to the job.</param>
	/// <param name="exectutionHandler">Executable item containing job's working methods.</param>
	/// <param name="overwrite">If set true will overwrite the existing job with same id and create new one.</param>
	/// <param name="token">Token to handle cancellation of asynchronous tasks.</param>
	/// <returns>
	/// A task representing the asynchronous operation with a <see cref="JobData"/> as a return value.
	/// Creates new <see cref="JobData"/> if job with same id wasn't found in the pool, the status of existing job
	/// is <see cref="JobStatus.Failed"/> or generated result is invalid; otherwise returns existing job.
	/// </returns>
	public async Task<JobData> ScheduleJobAsync(string id, ExecutableHandler exectutionHandler, bool overwrite, CancellationToken token)
	{
		if (overwrite)
		{
			_logger.LogDebug("Force overwrite flag was received.");
			await _jobStorage.RemoveJobAsync(id, token);
		}
		else
		{
			var existingJob = await _jobStorage.GetJobAsync(id, token);
			if (existingJob != null)
			{
				if (existingJob.Status is not JobStatus.Failed and not JobStatus.Cancelled)
				{
					_logger.LogDebug($"Retrieving existing job. Status: {existingJob.Status}");
					return existingJob;
				}

				_logger.LogDebug($"Removing existing job with status: {existingJob.Status}...");
				await _jobStorage.RemoveJobAsync(id, token);
			}
		}

		_logger.LogDebug("Creating new job...");
		return await CreateJobAsync(id, exectutionHandler, token);
	}
	public IAsyncEnumerable<JobData> EnumerateJobsAsync(CancellationToken token) => _jobStorage.EnumerateJobsAsync(token);
	public Task<JobData?> GetJobAsync(string id, CancellationToken token) => _jobStorage.GetJobAsync(id, token);
	public Task<bool> TryUpdateJobAsync(JobData jobData, CancellationToken token) => _jobStorage.TryUpdateJobAsync(jobData, token);
	public Task<bool> TryAddJobAsync(JobData jobData, CancellationToken token) => _jobStorage.TryAddJobAsync(jobData, token);

	public Task<JobData?> RemoveJobAsync(string id, CancellationToken token)
	{
		var job = _jobStorage.RemoveJobAsync(id, token);
		if (_activeWorkers.TryGetValue(id, out var worker))
		{
			worker?.Dispose();
		}

		return job;
	}

	/// <summary>
	/// Performs check of current job storage and sets status of all unfinished jobs to <see cref="JobStatus.Failed"/>.
	/// </summary>
	/// <returns>Tuple: (total jobs, total unfinished jobs).</returns>
	public async Task<(int Total, int Unfinished)> ResolveUnfinishedAsync(CancellationToken token)
	{
		int unfinished = 0;
		int total = 0;
		await foreach (var jobData in _jobStorage.EnumerateJobsAsync(token))
		{
			total++;
			if (jobData.Status is JobStatus.Pending or JobStatus.InProgress)
			{
				unfinished++;
				await jobData.SetStatusAsync(
					JobStatus.Failed,
					error: "Job was force set to Failed because it was Pending or InProgress on boot.",
					token: token);
			}
		}

		return (unfinished, total);
	}

	/// <summary>
	/// Performs check of current job storage and deletes all expired jobs.
	/// </summary>
	/// <returns>Tuple: (total jobs, total removed, total failed to remove).</returns>
	public async Task<(int TotalJobs, int TotalRemoved, int TotalFailed)> CheckForCleanupsAsync(CancellationToken token)
	{
		int totalRemoved = 0;
		int totalFailed = 0;
		int totalJobs = 0;
		await foreach (var jobData in _jobStorage.EnumerateJobsAsync(token))
		{
			totalJobs++;
			if (jobData.IsExpired())
			{
				if (await _jobStorage.RemoveJobAsync(jobData.Id, token) != null)
				{
					totalRemoved++;
				}
				else
				{
					totalFailed++;
				}
			}
		}

		return (totalJobs, totalRemoved, totalFailed);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_semaphoreParallelismLimiter.Dispose();
		_semaphoreQueueLimiter.Dispose();
	}

	/// <summary>
	/// Creates job asynchronously with given id and executable item.
	/// </summary>
	/// <param name="id">Id that will be assigned to the job.</param>
	/// <param name="executionHandler">Executable item, that will provide for the job's main work.</param>
	/// <param name="token">Token to handle cancellation of asynchronous tasks.</param>
	/// <returns>A task representing the asynchronous operation with a <see cref="JobData"/> as a return value.</returns>
	private async Task<JobData> CreateJobAsync(string id, ExecutableHandler executionHandler, CancellationToken token)
	{
		JobData jobData = JobData.Create(id, _jobConfiguration.JobExpireTime, _jobStorage);
		JobWorker jobWorker = new(executionHandler, _jobConfiguration.JobMaxRunTime);

		if (!await _jobStorage.TryAddJobAsync(jobData, token))
		{
			throw new InvalidOperationException($"Could not add job ({jobData}) to the pool");
		}

		_logger.LogDebug($"Successfully added job ({jobData}) to the storage {_jobStorage.GetType().Name}.");
		using var timeoutCts = new CancellationTokenSource(_jobConfiguration.EnqueueTimeout);
		try
		{
			using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token))
			{
				await _semaphoreQueueLimiter.WaitAsync(linkedCts.Token).ConfigureAwait(false);
			}

			_ = Task.Run(() => ExecuteJobAsync(jobData, jobWorker, token).ConfigureAwait(false), CancellationToken.None);
			return jobData;
		}
		catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
		{
			throw new EnqueueTimeoutException("Operation was cancelled when writing into the job queue due to timeout.");
		}
	}

	private async Task ExecuteJobAsync(JobData jobData, JobWorker jobWorker, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		try
		{
			if (jobData.Status != JobStatus.Pending)
			{
				_logger.LogError($"Job ({jobData}) wasn't in status \"{JobStatus.Pending}\" before execution. Aborting...");
				return;
			}

			await _semaphoreParallelismLimiter.WaitAsync(token).ConfigureAwait(false);

			_logger.LogDebug($"Initiating execution of a job ({jobData})");
			await jobData.SetStatusAsync(JobStatus.InProgress, token: token);

			_activeWorkers.TryAdd(jobData.Id, jobWorker);
			await jobWorker.ExecuteAsync(jobData, token).ConfigureAwait(false);

			_logger.LogInformation($"Job ({jobData}) was executed successfully");
			await jobData.SetStatusAsync(JobStatus.Success, token: token);
		}
		catch (OperationCanceledException ex)
		{
			await jobData.SetStatusAsync(JobStatus.Cancelled, $"Job's ({jobData}) execution was cancelled.", token: token);
			_logger.LogWarning(ex, $"Job's ({jobData}) execution was cancelled.");
		}
		catch (Exception ex)
		{
			await jobData.SetStatusAsync(JobStatus.Failed, $"Error occurred when executing a job ({jobData}): {ex.Message.TrimEnd('.')}. See logs for more details.", token: token);
			_logger.LogError(ex, $"Error occurred when executing a job ({jobData}): {ex.Message.TrimEnd('.')}");
		}
		finally
		{
			_activeWorkers.TryRemove(jobData.Id, out _);
			_semaphoreParallelismLimiter.Release();
			_logger.LogDebug($"Job ({jobData}) finished execution. Status: {jobData.Status}.");
		}
	}
}
