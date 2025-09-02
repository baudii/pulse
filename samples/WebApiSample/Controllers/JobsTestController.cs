using KK.Pulse.AspNetCore.Dto;
using KK.Pulse.Core;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api")]
public class JobsTestController(JobHandler jobHandler, ILogger<JobsTestController> logger) : ControllerBase
{
	[HttpPost]
	[Route("jobs")]
	public async Task<IActionResult> PostJob(CancellationToken httpToken, [FromQuery] int count = 16)
	{
		logger.LogInformation($"Processing POST request /jobs");
		var jobData = await jobHandler.ScheduleJobAsync(async (data, token) =>
		{
			for (int i = 0; i < count; ++i)
			{
				await data.SetResultAsync((string[])["path", $"{i} sec"], token);
				logger.LogInformation($"{i} seconds passed");
				await Task.Delay(1000, token);
			}
		}, httpToken);

		return Ok(new JobDto(jobData));
	}

	[HttpPost]
	[Route("jobs/multiple")]
	public async Task<IActionResult> PostJobs(CancellationToken httpToken, [FromQuery] int count = 16)
	{
		logger.LogInformation($"Processing POST request /jobs/multiple");
		for (int i = 0; i < count; ++i)
		{
			var s = i;
			await jobHandler.ScheduleJobAsync(async (data, token) =>
			{
				logger.LogInformation($"{s}");
				await Task.Delay(1000, token);
				logger.LogInformation($"{s} - Finished");
			}, httpToken);
		}

		return Ok();
	}

	[HttpGet]
	[Route("jobs/{id}")]
	public async Task<IActionResult> GetJob(string id, CancellationToken token)
	{
		logger.LogInformation($"Processing GET request /jobs/{id}");
		var job = await jobHandler.JobStorage.GetJobAsync(id, token);
		if (job == null)
		{
			return NotFound();
		}

		return Ok(new JobDto(job));
	}

	[HttpGet]
	[Route("jobs")]
	public async Task<IActionResult> GetJobs(CancellationToken token)
	{
		logger.LogInformation($"Processing GET request /jobs");
		List<JobDto> jobs = [];
		await foreach (var job in jobHandler.JobStorage.EnumerateJobsAsync(token))
		{
			jobs.Add(new JobDto(job));
		}

		return Ok(jobs);
	}

	[HttpGet]
	[Route("jobs/running")]
	public async Task<IActionResult> GetAliveJobs(CancellationToken token)
	{
		logger.LogInformation($"Processing GET request /jobs/running");
		List<JobDto> jobs = [];
		await foreach (var job in jobHandler.JobStorage.EnumerateJobsAsync(token))
		{
			if (job.Status == JobStatus.InProgress)
			{
				jobs.Add(new JobDto(job));
			}
		}

		return Ok(jobs);
	}

	[HttpDelete]
	[Route("jobs")]
	public async Task<IActionResult> DeleteAllJobs(CancellationToken token)
	{
		logger.LogInformation($"Processing DELETE request /jobs");

		List<JobDto> jobs = [];
		await foreach (var job in jobHandler.JobStorage.EnumerateJobsAsync(token))
		{
			var removed = await jobHandler.JobStorage.RemoveJobAsync(job.Id, token);
			if (removed != null)
			{
				jobs.Add(new JobDto(removed));
			}
		}

		return Ok(jobs);
	}

	[HttpDelete]
	[Route("jobs/{id}")]
	public async Task<IActionResult> DeleteAllJobs(string id, CancellationToken token)
	{
		logger.LogInformation($"Processing DELETE request /jobs/{id}");
		var removed = await jobHandler.JobStorage.RemoveJobAsync(id, token);
		if (removed == null)
		{
			return NotFound();
		}

		return Ok(new JobDto(removed));
	}
}
