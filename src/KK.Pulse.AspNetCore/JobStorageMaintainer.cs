using KK.Pulse.Core;
using KK.Pulse.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KK.Pulse.AspNetCore;

internal class JobStorageMaintainer(JobHandler jobHandler, IOptions<PulseConfig> configuration, ILogger<JobStorageMaintainer> logger) : BackgroundService
{
	private readonly TimeSpan _maintainInterval = configuration.Value.StorageMaintainInterval;
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation($"Executing initial storage maintenance check...");
		var (unfinished, total) = await jobHandler.ResolveUnfinishedAsync(stoppingToken);
		logger.LogInformation($"Maintenance finished. Total jobs in storage: {total}. Total unfinished: {unfinished}");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				logger.LogInformation($"Initiating cleanup of jobs dictionary...");
				var (totalJobs, totalRemoved, totalFailed) = await jobHandler.CheckForCleanupsAsync(stoppingToken);
				int totalAttempted = totalRemoved + totalFailed;
				logger.LogInformation($"Cleanup completed. Total jobs found: {totalJobs}. Attempted to remove: {totalAttempted}. Succeeded {totalRemoved}. Failed: {totalFailed}.");

				await Task.Delay(_maintainInterval, stoppingToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"Unexpected error occured in {GetType().Name}.");
				await Task.Delay(_maintainInterval, stoppingToken);
			}
		}
	}
}
