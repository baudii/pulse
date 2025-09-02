using KK.Pulse.Core;

namespace KK.Pulse.AspNetCore.Dto;

/// <summary>
/// Represents data transfer object for jobs collection.
/// </summary>
public sealed class JobCollectionDto(IEnumerable<JobData> jobs)
{
	/// <summary>
	/// Gets or sets a collection of active jobs.
	/// </summary>
	public IEnumerable<JobDto> Jobs { get; set; } = jobs.Select(job => new JobDto(job));
}
