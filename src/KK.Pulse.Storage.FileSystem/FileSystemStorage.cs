using KK.Pulse.Core;
using KK.Pulse.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace KK.Pulse.Storage.FileSystem;

public class FileSystemStorage : IJobStorage
{
	private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
	private static SemaphoreSlim GetLock(string path) => _locks.GetOrAdd(Normalize(path), _ => new SemaphoreSlim(1, 1));

	private readonly string _jobStoragePath;

	private readonly JsonSerializerOptions _jsonSerializerOptions;

	private readonly ILogger<FileSystemStorage> _logger;

	public FileSystemStorage(IOptions<PulseConfig> jobOptions, ILogger<FileSystemStorage> logger)
	{
		var jobConfiguration = jobOptions.Value;
		ArgumentNullException.ThrowIfNull(jobConfiguration.FileSystemStorage);

		_logger = logger;
		_jobStoragePath = jobConfiguration.FileSystemStorage;
		_jsonSerializerOptions = new JsonSerializerOptions()
		{
			WriteIndented = true
		};
	}

	public async IAsyncEnumerable<JobData> EnumerateJobsAsync([EnumeratorCancellation] CancellationToken token)
	{
		var filePaths = Directory.EnumerateFiles(_jobStoragePath, "*.jb", SearchOption.TopDirectoryOnly);
		foreach (var path in filePaths)
		{
			yield return await LoadJobFromFileAsync(path, token);
		}
	}

	public async Task<bool> TryAddJobAsync(JobData jobData, CancellationToken token)
	{
		string filePath = GetJobFilePath(jobData.Id);
		if (!File.Exists(filePath))
		{
			await SaveJobToFileAsync(jobData, filePath, token);
			return true;
		}

		return false;
	}

	public async Task<bool> TryUpdateJobAsync(JobData jobData, CancellationToken token)
	{
		string filePath = GetJobFilePath(jobData.Id);
		if (File.Exists(filePath))
		{
			await SaveJobToFileAsync(jobData, filePath, token);
			return true;
		}

		return false;
	}

	public async Task<JobData?> GetJobAsync(string id, CancellationToken token)
	{
		string filePath = GetJobFilePath(id);
		if (File.Exists(filePath))
		{
			var jobData = await LoadJobFromFileAsync(filePath, token);
			await jobData.TouchAsync(token);
			return jobData;
		}

		return null;
	}

	public async Task<JobData?> RemoveJobAsync(string id, CancellationToken token)
	{
		string filePath = GetJobFilePath(id);
		if (File.Exists(filePath))
		{
			var jobData = await LoadJobFromFileAsync(filePath, token);
			File.Delete(filePath);
			return jobData;
		}

		return null;
	}

	private async Task SaveJobToFileAsync(JobData jobData, string filePath, CancellationToken token)
	{
		_logger.LogDebug($"Saving job ({jobData}) to file {filePath}");
		var gate = GetLock(filePath);
		var json = JsonSerializer.Serialize(jobData, _jsonSerializerOptions);

		var tmpFilePath = $"{filePath}.tmp";
		await File.WriteAllTextAsync(tmpFilePath, json, token);

		try
		{
			await gate.WaitAsync(token);
			if (File.Exists(filePath))
			{
				_logger.LogDebug($"Replacing file ({jobData}) from {tmpFilePath} to {filePath}");
				File.Replace(tmpFilePath, filePath, null);
			}
			else
			{
				_logger.LogDebug($"Moving file ({jobData}) from {tmpFilePath} to {filePath}");
				File.Move(tmpFilePath, filePath);
			}
		}
		finally
		{
			_logger.LogDebug($"Releasing semaphore ({jobData})");
			gate.Release();
		}
	}

	private async Task<JobData> LoadJobFromFileAsync(string filePath, CancellationToken token)
	{
		var gate = GetLock(filePath);
		try
		{
			await gate.WaitAsync(token);
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var jobData = await JsonSerializer.DeserializeAsync<JobData>(fs, _jsonSerializerOptions, token)
				?? throw new InvalidOperationException($"Could not deserialize data from path '{filePath}' to a valid {typeof(JobData).Name}");

			jobData.AttachStorage(this);
			return jobData;
		}
		finally
		{
			gate.Release();
		}
	}

	private string GetJobFilePath(string id) => Path.Combine(_jobStoragePath, GetJobFileName(id));

	private static string Normalize(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

	private static string GetJobFileName(string id) => $"{id}.jb";
}
