using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.Common;

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
        // Create New Monday Options.
        MondayOptions mondayOptions = new();

        // Invoke Delegate That Will Assign The Options To The Monday Options.
        options.Invoke(mondayOptions);

        // Add Monday Options To The Service Collection.
        services.TryAddSingleton(mondayOptions);

        // Add Monday Client To The Service Collection.
        services.TryAddSingleton<IMondayClient>();

        // Return The Service Collection.
        return services;
    }
}
