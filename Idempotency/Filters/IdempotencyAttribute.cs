using Idempotency.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Idempotency.Filters;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class IdempotencyAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var idempotencyCache = (IDistributedCacheService)serviceProvider.GetService(typeof(IDistributedCacheService));
        var idempotencyAttributeFilter = new IdempotencyFilterAttribute(idempotencyCache);
        return idempotencyAttributeFilter;
    }
}
