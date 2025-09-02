using System.Text.Json.Serialization;

namespace KK.Pulse.Core;

/// <summary>
/// Predefined states of job's execution progress status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
	/// <summary>
	/// Job's execution hasn't started and is in queue.
	/// </summary>
	Pending,

	/// <summary>
	/// Job's execution has started and is in progress.
	/// </summary>
	InProgress,

	/// <summary>
	/// Job's execution finished successfully.
	/// </summary>
	Success,

	/// <summary>
	/// Job's execution was cancelled.
	/// </summary>
	Cancelled,

	/// <summary>
	/// Job's execution failed.
	/// </summary>
	Failed
}
