using Idempotency.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Idempotency.Filters;

public class IdempotencyFilterAttribute : IAsyncActionFilter, IAsyncResultFilter, IAsyncExceptionFilter
{
    private readonly IDistributedCacheService _idempotencyCache;
    private IdempotencyLayer _idempotency;

    public IdempotencyFilterAttribute(IDistributedCacheService idempotencyCache)
    {
        _idempotencyCache = idempotencyCache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        _idempotency = new IdempotencyLayer(_idempotencyCache);

        await _idempotency.ProcessPreAction(context);

        if (context.Result == null)
            await next();
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        await _idempotency.ResetIdempotency();
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        await next();

        if (_idempotency != null)
            await _idempotency.ProcessPostAction(context);
    }
}

