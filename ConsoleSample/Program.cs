using KK.Pulse.Core;
using KK.Pulse.Core.Configuration;
using KK.Pulse.Storage.InMemory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;



var cts = new CancellationTokenSource();
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
	cts.Cancel();
	cts.Dispose();
};

JobHandler jobHandler = CreateJobHandler();
List<JobData> jobs = [];

int totalJobs = args.Length > 0 ? int.Parse(args[0]) : 5;

_ = Task.Run(async () =>
{
	await Task.Delay(100, cts.Token);

	for (int i = 0; i < totalJobs; ++i)
	{
		jobs.Add(await jobHandler.ScheduleJobAsync(async (job, token) =>
		{
			var captured = i + 1;
			Console.WriteLine($"---- Thread #{captured}. New Job started ({job}). Created time: {job.CreatedTime} ----");
			for (int j = 0; j < 25; ++j)
			{
				if (job.Result == null)
					await job.SetResultAsync(0, token);
				else
					await job.SetResultAsync((int)job.Result + 1, token);

				Console.WriteLine($"Job #{captured}. Executing job ({job}). Current result: ({job.Result})");
				await Task.Delay(1000, token);
			}
		}, cts.Token));

		await Task.Delay(10000, cts.Token);
	}
}, cts.Token);

for (int i = 0; i < totalJobs; ++i)
{
	bool msgShown = false;
	while (i > jobs.Count - 1)
	{
		if (!msgShown)
		{
			Console.WriteLine($"Waiting for the next job...");
			msgShown = true;
		}

		await Task.Delay(100, cts.Token);
	}

	Console.WriteLine("Press Enter to cancel the job.");
	while (Console.KeyAvailable)
		_ = Console.ReadKey(intercept: true);

	while (true)
	{
		var key = Console.ReadKey(intercept: true);
		if (key.Key == ConsoleKey.Enter)
		{
			break;
		}
	}

	await jobHandler.RemoveJobAsync(jobs[i].Id, cts.Token);
	Console.WriteLine($"=== Job ({jobs[i]}) removed ====");
}

static JobHandler CreateJobHandler()
{
	var logFacotory = LoggerFactory.Create(builder =>
	{
		builder.AddSimpleConsole();
	});
	var logger = logFacotory.CreateLogger<JobHandler>();

	var jobStorage = new InMemoryStorage();
	var jobConfiguration = new PulseConfig() { MaxDegreeParallelism = 4 };
	var options = Options.Create(jobConfiguration);
	var jobHandler = new JobHandler(jobStorage, logger, options);
	return jobHandler;
}