using KK.Pulse.Core;
using Microsoft.Extensions.DependencyInjection;

namespace KK.Pulse.Storage.FileSystem;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddFileSystemStorage(this IServiceCollection services)
	{
		services.AddSingleton<IJobStorage, FileSystemStorage>();
		return services;
	}
}
