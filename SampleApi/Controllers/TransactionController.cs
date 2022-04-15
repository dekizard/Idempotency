using Idempotency.Filters;
using Microsoft.AspNetCore.Mvc;
using SampleApi.Models;
using System.Net;

namespace SampleApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionController : ControllerBase
{    
    private readonly ILogger<TransactionController> _logger;
    public TransactionController(ILogger<TransactionController> logger)
    {
        _logger = logger;
    }

    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CreateTransactionResponse), (int)HttpStatusCode.OK)]
    [HttpPost]
    [Idempotency]
    public IActionResult Create([FromBody] CreateTransactionRequest request)
    {
        if (request == null)
        {
            return BadRequest("Model is not valid.");
        }

        var result = new CreateTransactionResponse()
        {
            
            Id = Guid.NewGuid(),
            Status = "Pending"
        };
        return Ok(result);
    }    
}
