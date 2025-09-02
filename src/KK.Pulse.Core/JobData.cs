using System;
using System.Threading;
using System.Threading.Tasks;

namespace KK.Pulse.Core;

/// <summary>
/// Represents an asynchronous content generation job with life cycle management.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobData"/> class.
/// </remarks>
/// <param name="id">Id assigned to this job by <see cref="JobHandler"/>.</param>
/// <param name="config">Jobs configuration object.</param>
public sealed class JobData
{
	private readonly Lock _statusLock = new();
	private readonly Lock _resultLock = new();
	private readonly Lock _accessTimeLock = new();

	private IJobStorage _storage = default!;

	internal JobData(
		string id,
		JobStatus status,
		DateTime createdTime,
		long jobExpireTime,
		string? error,
		object? result,
		DateTime lastAccessTime)
	{
		Id = id;
		Status = status;
		CreatedTime = createdTime;
		JobExpireTime = jobExpireTime;
		Error = error;
		Result = result;
		LastAccessTime = lastAccessTime;
	}

	internal static JobData Create(string id, TimeSpan jobExpireTime, IJobStorage storage)
	{
		var jobData = new JobData(id, JobStatus.Pending, DateTime.Now, jobExpireTime.Ticks, null, null, DateTime.UtcNow)
		{
			_storage = storage
		};

		return jobData;
	}

	/// <summary>
	/// Gets the unique id of the job.
	/// </summary>
	public string Id { get; }

	/// <summary>
	/// Gets the current status of the generation job.
	/// </summary>
	public JobStatus Status { get; private set; }

	/// <summary>
	/// Gets the time when the job was created.
	/// </summary>
	public DateTime CreatedTime { get; }

	/// <summary>
	/// The last time this job was accessed or touched.
	/// </summary>
	public DateTime LastAccessTime { get; private set; }

	/// <summary>
	/// Maximum idle time (in minutes) before a job is considered timed out and eligible for cleanup.
	/// </summary>
	public long JobExpireTime { get; }

	/// <summary>
	/// Gets the amount of time that was spent executing this job.
	/// </summary>
	public long ExecutionTime { get; set; }

	/// <summary>
	/// Gets the error message if the job failed, otherwise null.
	/// </summary>
	public string? Error { get; private set; }

	/// <summary>
	/// Gets or sets the result of a job.
	/// </summary>
	public object? Result { get; private set; }

	public void AttachStorage(IJobStorage storage) => _storage = storage;

	/// <summary>
	/// Updates last accessed time.
	/// </summary>
	public Task TouchAsync(CancellationToken token)
	{
		lock (_accessTimeLock)
		{
			LastAccessTime = DateTime.UtcNow;
			return OnUpdateAsync(token);
		}
	}

	/// <summary>
	/// Checks whether job exceeded it's maximum lifetime.
	/// </summary>
	/// <returns><see langword="true"/> if maximum lifetime was reached; otherwise <see langword="false"/></returns>
	public bool IsExpired()
	{
		return DateTime.UtcNow - LastAccessTime >= TimeSpan.FromTicks(JobExpireTime);
	}

	public Task SetResultAsync(object result, CancellationToken token)
	{
		lock (_resultLock)
		{
			Result = result;
			return OnUpdateAsync(token);
		}
	}

	/// <summary>
	/// Sets this job a new status. This method is thread safe.
	/// </summary>
	/// <param name="status">New status of the job.</param>
	/// <param name="error">Error message that is provided when status is <see cref="JobStatus.Failed"/></param>
	public Task SetStatusAsync(JobStatus status, string? error = null, CancellationToken token = default)
	{
		lock (_statusLock)
		{
			Status = status;
			Error = error;
			return OnUpdateAsync(token);
		}
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return Id[..8];
	}

	private async Task OnUpdateAsync(CancellationToken token)
	{
		await _storage.TryUpdateJobAsync(this, token);
	}
}