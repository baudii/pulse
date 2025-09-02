using KK.Pulse.Core;
using Microsoft.Extensions.DependencyInjection;

namespace KK.Pulse.Storage.InMemory;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInMemoryStorage(this IServiceCollection services)
	{
		services.AddSingleton<IJobStorage, InMemoryStorage>();
		return services;
	}
}
