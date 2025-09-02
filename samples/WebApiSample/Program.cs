using Isle.Configuration;
using KK.Pulse.AspNetCore;
using KK.Pulse.Storage.InMemory;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Host.UseSerilog((context, configuration) =>
	configuration.ReadFrom.Configuration(context.Configuration));
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

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();
