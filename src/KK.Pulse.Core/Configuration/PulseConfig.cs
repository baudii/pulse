using System;

namespace KK.Pulse.Core.Configuration;

/// <summary>
/// Configuration for jobs project.
/// </summary>
public class PulseConfig
{
	/// <summary>
	/// Gets or sets a persistant storaget of jobs.
	/// </summary>
	public string? FileSystemStorage { get; set; } = null;

	public bool UseRedisStorage { get; set; } = false;

	/// <summary>
	/// Gets or sets a maximum amount of tasks to run in parallel.
	/// </summary>
	public int MaxDegreeParallelism { get; set; } = 8;

	/// <summary>
	/// Gets or sets a maximum amount of tasks to run in parallel.
	/// </summary>
	public int MaxJobQueueSize { get; set; } = 100;

	/// <summary>
	/// Gets or sets the amount of time after which the job will be considered expired.
	/// </summary>
	public TimeSpan JobExpireTime { get; set; } = TimeSpan.FromMinutes(30);

	/// <summary>
	/// Gets or sets the amount of time after which the job's execution must be interrupted.
	/// </summary>
	public TimeSpan JobMaxRunTime { get; set; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Gets or sets the amount of time after which the job enqueue functionality will be considered timed out.
	/// </summary>
	public TimeSpan EnqueueTimeout { get; set; } = TimeSpan.FromSeconds(15);

	/// <summary>
	/// Gets or sets the amount of time after which the job enqueue functionality will be considered timed out.
	/// </summary>
	public TimeSpan StorageMaintainInterval { get; set; } = TimeSpan.FromMinutes(1);
}
