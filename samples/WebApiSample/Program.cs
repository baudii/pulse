using Isle.Configuration;
using KK.Pulse.AspNetCore;
using KK.Pulse.Storage.InMemory;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Logging.ClearProviders();
builder.Logging.AddNLogWeb();

builder.Services
	.AddPulse(options =>
	{
		options.MaxDegreeParallelism = 8;
		options.UseRedisStorage = true;
		options.StorageMaintainInterval = TimeSpan.FromMinutes(5);
	})
	.AddInMemoryStorage();

IsleConfiguration.Configure(builder => builder.ConfigureExtensionsLogging());

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();
