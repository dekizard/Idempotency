using Idempotency.Interfaces;
using Idempotency.Options;
using Idempotency.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Idempotency.Extensions;

public static class DistributedCacheExtension
{
    public static IServiceCollection AddDistributedCache(this IServiceCollection servcies, Action<IdempotencyOptions> configureOptions)
    {
        if (servcies == null)
        {
            throw new ArgumentNullException(nameof(servcies));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        servcies.Configure(configureOptions);
        servcies.TryAddSingleton<IDistributedCacheService, DistributedCacheService>();
        
        return servcies;
    }
}
