using Idempotency.Extensions;
using Idempotency.Options;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//For testing purpose memory caching can be sufficient
builder.Services.AddDistributedMemoryCache();

//For production (uncomment the below redis registration and comment the above line) Redis distributed cache service could be used.
//builder.Services.AddStackExchangeRedisCache(opt =>
//{
//    opt.Configuration = configuration.GetConnectionString("Redis");
//    opt.InstanceName = configuration.GetSection("Idempotency")["KeyPrefix"];
//});

builder.Services.AddDistributedCache(opt => 
{
    opt.ExpirationInHours = Convert.ToInt32(configuration.GetSection("Idempotency")["ExpireInHours"]);
});

//TODO register redis or inmemeory

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
