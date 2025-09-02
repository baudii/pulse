using KK.Pulse.Core;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace KK.Pulse.Storage.Redis;

public class RedisStorage : IJobStorage
{
	private readonly JsonSerializerOptions _jsonSerializerOptions;
	private readonly IConnectionMultiplexer _redis;
	private static string JobKey(string id) => $"job:{id}";
	private const string JobsKey = "jobs:all";

	public RedisStorage(IConnectionMultiplexer connection)
	{
		_redis = connection;
		_jsonSerializerOptions = new JsonSerializerOptions()
		{
			WriteIndented = true
		};
	}

	public async Task<JobData?> GetJobAsync(string id, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var db = _redis.GetDatabase();
		var redisRes = await db.ExecuteAsync("JSON.GET", JobKey(id));
		return redisRes.IsNull ? null : JsonSerializer.Deserialize<JobData>((string)redisRes!, _jsonSerializerOptions);
	}

	public async Task<JobData?> RemoveJobAsync(string id, CancellationToken token)
	{
		var db = _redis.GetDatabase();
		var transaction = db.CreateTransaction();
		var getTask = transaction.ExecuteAsync("JSON.GET", JobKey(id)).ConfigureAwait(false);
		_ = transaction.ExecuteAsync("JSON.DEL", JobKey(id)).ConfigureAwait(false);
		_ = transaction.SortedSetRemoveAsync(JobsKey, id).ConfigureAwait(false);
		if (!await transaction.ExecuteAsync().ConfigureAwait(false))
		{
			return null;
		}

		token.ThrowIfCancellationRequested();
		var getRes = await getTask;
		return !getRes.IsNull ? JsonSerializer.Deserialize<JobData?>((string)getRes!, _jsonSerializerOptions) : null;
	}

	public Task<bool> TryUpdateJobAsync(JobData jobData, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var db = _redis.GetDatabase();
		var transaction = db.CreateTransaction();
		var key = JobKey(jobData.Id);
		transaction.AddCondition(Condition.KeyExists(key));

		var json = JsonSerializer.Serialize(jobData, _jsonSerializerOptions);
		_ = transaction.ExecuteAsync("JSON.SET", key, "$", json);
		_ = transaction.SortedSetAddAsync(JobsKey, jobData.Id, jobData.LastAccessTime.Ticks);

		return transaction.ExecuteAsync();
	}

	public Task<bool> TryAddJobAsync(JobData jobData, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var db = _redis.GetDatabase();
		var transaction = db.CreateTransaction();
		var key = JobKey(jobData.Id);
		transaction.AddCondition(Condition.KeyNotExists(key));

		var json = JsonSerializer.Serialize(jobData, _jsonSerializerOptions);
		_ = transaction.ExecuteAsync("JSON.SET", key, "$", json);
		_ = transaction.SortedSetAddAsync(JobsKey, jobData.Id, jobData.CreatedTime.Ticks);

		return transaction.ExecuteAsync();
	}

	public async IAsyncEnumerable<JobData> EnumerateJobsAsync([EnumeratorCancellation] CancellationToken token)
	{
		var db = _redis.GetDatabase();
		var jobIds = await db.SortedSetRangeByScoreAsync(JobsKey);
		foreach (var id in jobIds)
		{
			token.ThrowIfCancellationRequested();
			var jobJson = await db.ExecuteAsync("JSON.GET", JobKey((string)id!));
			if (jobJson == null || jobJson.IsNull)
			{
				continue;
			}

			var jobData = JsonSerializer.Deserialize<JobData>((string)jobJson!, _jsonSerializerOptions);
			if (jobData == null)
			{
				continue;
			}

			yield return jobData;
			await Task.Yield();
		}
	}
}
