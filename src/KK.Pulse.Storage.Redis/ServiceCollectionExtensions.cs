using KK.Pulse.Core;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KK.Pulse.Storage.Redis;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRedisStorage(this IServiceCollection services, string? connectionString)
	{
		ArgumentException.ThrowIfNullOrEmpty(connectionString);

		services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
		services.AddSingleton<IJobStorage, RedisStorage>();
		return services;
	}
}
