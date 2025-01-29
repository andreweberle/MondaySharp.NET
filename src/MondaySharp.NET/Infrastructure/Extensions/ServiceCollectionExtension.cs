using Microsoft.Extensions.DependencyInjection;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Infrastructure.Persistence;

namespace MondaySharp.NET.Infrastructure.Extensions;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection TryAddMondayClient(this IServiceCollection services, Action<MondayOptions> options)
    {
        // Add Logging To The Service Collection.
        services.AddLogging();

        // Add Monday Client To The Service Collection.
        services.AddSingleton<IMondayClient, MondayClient>(sp =>
        {
            return new MondayClient(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MondayClient>>(),
                options);
        });

        // Return The Service Collection.
        return services;
    }
}