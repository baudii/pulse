using KK.Pulse.Core;
using System.Globalization;
using System.Text.Json.Serialization;

namespace KK.Pulse.AspNetCore.Dto;

/// <summary>
/// Generation job data transfer object.
/// </summary>
public readonly struct JobDto
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JobDto"/> struct from existing <see cref="JobData"/>.
	/// </summary>
	/// <param name="job">Job to get data from.</param>
	public JobDto(JobData job)
	{
		Id = job.Id;
		Status = job.Status;
		Error = job.Error;
		Created = job.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		Duration = job.ExecutionTime > 0 ? TimeSpan.FromTicks(job.ExecutionTime).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture) : null;
		Result = job.Result;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JobDto"/> struct from existing <see cref="JobData"/>.
	/// </summary>
	/// <param name="job">Job to get data from.</param>
	public JobDto(string id, JobStatus status, string error, DateTime created, TimeSpan duration, object? result = null)
	{
		Id = id;
		Status = status;
		Error = error;
		Created = created.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		Duration = duration != default ? duration.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture) : null;
		Result = result;
	}

	/// <summary>
	/// Gets the identifier of original job.
	/// </summary>
	public string Id { get; }

	/// <summary>
	/// Gets the status of original job.
	/// </summary>
	public JobStatus Status { get; }

	/// <summary>
	/// Gets the time when job was created.
	/// </summary>
	public string Created { get; }

	/// <summary>
	/// Gets the amount of time that was spent executing this job.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Duration { get; }

	/// <summary>
	/// Gets the error message generated when processing original job.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Error { get; }

	/// <summary>
	/// Gets the result of processing the original job.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public object? Result { get; }
}
