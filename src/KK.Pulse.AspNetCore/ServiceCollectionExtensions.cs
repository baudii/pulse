using KK.Pulse.Core;
using KK.Pulse.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KK.Pulse.AspNetCore;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPulse(this IServiceCollection services, Action<PulseConfig> options)
	{
		ArgumentNullException.ThrowIfNull(options);
		services.Configure(options);
		services.AddJobHandler();
		return services;
	}

	public static IServiceCollection AddPulse(this IServiceCollection services, IConfigurationSection section)
	{
		ArgumentNullException.ThrowIfNull(section);
		services.Configure<PulseConfig>(section);
		services.AddJobHandler();
		return services;
	}

	public static IServiceCollection AddPulse(this IServiceCollection services)
	{
		services.Configure<PulseConfig>(options => { });
		services.AddJobHandler();
		return services;
	}

	private static IServiceCollection AddJobHandler(this IServiceCollection services)
	{
		services.AddSingleton<JobHandler>();
		services.AddHostedService<JobStorageMaintainer>();
		return services;
	}
}
