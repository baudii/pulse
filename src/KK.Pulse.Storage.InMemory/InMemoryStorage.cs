using KK.Pulse.Core;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace KK.Pulse.Storage.InMemory;

public class InMemoryStorage : IJobStorage
{
	private readonly ConcurrentDictionary<string, JobData> _jobs = new();

	public async Task<JobData?> GetJobAsync(string id, CancellationToken token)
	{
		if (_jobs.TryGetValue(id, out var jobData))
		{
			await jobData.TouchAsync(token);
			return jobData;
		}

		return null;
	}

	public Task<JobData?> RemoveJobAsync(string id, CancellationToken token)
	{
		if (_jobs.TryRemove(id, out var jobData))
		{
			return Task.FromResult<JobData?>(jobData);
		}

		return Task.FromResult<JobData?>(null);
	}

	public Task<bool> TryUpdateJobAsync(JobData jobData, CancellationToken token)
	{
		if (!_jobs.TryGetValue(jobData.Id, out var existingJob))
		{
			return Task.FromResult(false);
		}

		while (true)
		{
			token.ThrowIfCancellationRequested();
			if (_jobs.TryUpdate(jobData.Id, jobData, existingJob))
			{
				break;
			}
		}

		return Task.FromResult(true);
	}

	public Task<bool> TryAddJobAsync(JobData jobData, CancellationToken token)
	{
		return Task.FromResult(_jobs.TryAdd(jobData.Id, jobData));
	}

	public async IAsyncEnumerable<JobData> EnumerateJobsAsync([EnumeratorCancellation] CancellationToken token)
	{
		foreach (var job in _jobs.Values)
		{
			yield return job;
			await Task.Yield();
			token.ThrowIfCancellationRequested();
		}
	}
}
