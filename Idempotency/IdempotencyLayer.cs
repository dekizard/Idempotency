using Idempotency.Helpers;
using Idempotency.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Idempotency;

public class IdempotencyLayer
{
    private const string HeaderIdempotencyKeyName = "Idempotency-Key";
    private readonly IDistributedCacheService _distributedCache;
    private IDictionary<string, object> _actionArguments;

    private string _idempotencyKey = string.Empty;

    private bool _isProcessPreActionDone;
    private bool _isResultCreatedFromCache;

    public IdempotencyLayer(IDistributedCacheService distributedCache)
    {
        _distributedCache = distributedCache ??
                            throw new ArgumentException(
                                $"{nameof(IDistributedCacheService)} service is not registered.");
    }

    public async Task ProcessPreAction(ActionExecutingContext context)
    {
        if (!CanPerformIdempotency(context.HttpContext.Request))
        {
            return;
        }

        _idempotencyKey = GetIdempotencyKey(context.HttpContext.Request);
        _actionArguments = context.ActionArguments;

        var requestId = Guid.NewGuid();
        var cachedDataBytes = await _distributedCache.GetOrSet(
            _idempotencyKey,
            CreateProcessingCashingRequestMarker(requestId));

        var cachedData = cachedDataBytes.DecompressAndDeserialize();
        if (cachedData.ContainsKey("ProcessingCashingRequest"))
        {
            if (ExistConcurrentRequestsWithSameIdempotencyKey(cachedData["ProcessingCashingRequest"], requestId))
            {
                context.Result =
                    new ConflictObjectResult(
                        $"Conflict due to concurrent requests with the same idempotency key {_idempotencyKey}.");
                return;
            }

            _isProcessPreActionDone = true;
            return;
        }

        if (CacheContainsRequestWithSameIdempotencyKey(cachedData["Request.DataHash"],
                context.HttpContext.Request.Path))
        {
            context.Result = new BadRequestObjectResult(
                $"The Idempotency key {_idempotencyKey} was used in one of the previous requests.");
            return;
        }

        CreateActionResultFromCachedResult(context, cachedData["Context.Result"]);
        AddCachedHeadersToResponse(context, cachedData["Response.Headers"]);

        _isResultCreatedFromCache = true;
        _isProcessPreActionDone = true;
    }

    private static bool ExistConcurrentRequestsWithSameIdempotencyKey(object cachedRequestId, Guid requestId)
    {
        return cachedRequestId.ToString() != requestId.ToString();
    }

    private bool CacheContainsRequestWithSameIdempotencyKey(object cachedRequestDataHash, PathString requestPath)
    {
        var currentRequestDataHash = HashRequestData(requestPath);
        return cachedRequestDataHash.ToString() != currentRequestDataHash;
    }

    private static void CreateActionResultFromCachedResult(ActionExecutingContext context, object cachedResult)
    {
        var resultObjects = (Dictionary<string, object>)cachedResult;
        var contextResultType = Type.GetType(resultObjects["ResultType"].ToString() ?? string.Empty);
        if (contextResultType == null)
        {
            throw new Exception($"Return type {resultObjects["ResultType"]} is not recognized.");
        }

        if (contextResultType.BaseType == typeof(ObjectResult))
        {
            var value = resultObjects["ResultValue"];
            var ctor = contextResultType.GetConstructor(new[] { typeof(object) });
            if (ctor != null)
            {
                context.Result = (IActionResult)ctor.Invoke(new[] { value });
                return;
            }
        }

        throw new NotImplementedException($"InitializeActionResult - Not implemented type {contextResultType}.");
    }

    private static void AddCachedHeadersToResponse(ActionContext context, object cachedResponseHeaders)
    {
        var headerKeyValues = (Dictionary<string, List<string>>)cachedResponseHeaders;
        if (headerKeyValues == null) return;
        context.HttpContext.Response.Headers.Add("Content-Type", "application/json");
        foreach (var (key, value) in headerKeyValues)
        {
            if (!context.HttpContext.Response.Headers.ContainsKey(key))
            {
                context.HttpContext.Response.Headers.Add(key, value.ToArray());
            }
        }
    }

    public async Task ProcessPostAction(ResultExecutingContext context)
    {
        if (!_isProcessPreActionDone || _isResultCreatedFromCache)
        {
            return;
        }

        var cacheDataBytes = GenerateCacheDataFromContext(context);
        await _distributedCache.Set(_idempotencyKey, cacheDataBytes);
    }

    private bool CanPerformIdempotency(HttpRequest httpRequest)
    {
        return httpRequest.Method == HttpMethods.Post && !_isProcessPreActionDone;
    }

    private static string GetIdempotencyKey(HttpRequest httpRequest)
    {
        if (!httpRequest.Headers.TryGetValue(HeaderIdempotencyKeyName, out var idempotencyKeys))
        {
            throw new ArgumentException($"The Idempotency header key value is not found. Header name: {HeaderIdempotencyKeyName}");
        }

        if (string.IsNullOrEmpty(idempotencyKeys.FirstOrDefault()))
        {
            throw new ArgumentNullException($"Idempotency value is not found. Header name: {HeaderIdempotencyKeyName}");
        }

        if (idempotencyKeys.Count > 1)
        {
            throw new ArgumentException($"Multiple Idempotency keys were found. Header name: {HeaderIdempotencyKeyName}");
        }

        return idempotencyKeys.ToString();
    }

    private static byte[] CreateProcessingCashingRequestMarker(Guid guid)
    {
        Dictionary<string, object> requestId = new()
        {
            { "ProcessingCashingRequest", guid }
        };

        return requestId.SerializeAndCompress();
    }

    private byte[] GenerateCacheDataFromContext(ResultExecutingContext context)
    {
        Dictionary<string, object> cacheData = new()
        {
            { "Request.DataHash", HashRequestData(context.HttpContext.Request.Path) },
            { "Response.Headers", PrepareResponseHeaders(context.HttpContext.Response) }
        };

        var resultObjects = new Dictionary<string, object>
        {
            {"ResultType", context.Result.GetType().AssemblyQualifiedName}
        };
        if (context.Result is ObjectResult objectResult)
        {
            resultObjects.Add("ResultValue", objectResult.Value);
        }
        else
        {
            throw new NotImplementedException($"Not implemented type result type {context.GetType()}.");
        }

        cacheData.Add("Context.Result", resultObjects);
        var serializedCacheData = cacheData.SerializeAndCompress();

        return serializedCacheData;
    }

    private static Dictionary<string, List<string>> PrepareResponseHeaders(HttpResponse response)
    {
        var responseHeaders = response.Headers
            .Where(h => !GetExcludedHeaderKeys().Contains(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToList());

        responseHeaders.Add("Cached-Response", new List<string> { "true" });

        return responseHeaders;
    }

    private static IEnumerable<string> GetExcludedHeaderKeys()
    {
        return new List<string> { "Transfer-Encoding" };
    }

    private string HashRequestData(PathString requestPath)
    {
        var requestParams = _actionArguments.Select(argument => argument.Value).ToList();
        if (requestPath.HasValue)
        {
            requestParams.Add(requestPath.ToString());
        }

        return Utils.GetHash(JsonConvert.SerializeObject(requestParams));
    }

    public async Task ResetIdempotency()
    {
        await _distributedCache.Remove(_idempotencyKey);
    }
}

