using ExampleWebApi.Extensions;
using MongoDB.Driver;
using MongoObject.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

#region BasicSetup
// Make sure you register a IMonogClient
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var mongoConnectionUrl = new MongoUrl("mongodb://localhost:27018/?directConnection=true");
    var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);

    return new MongoClient(mongoClientSettings);
});

builder.Services.AddMongoObject((builder, options) =>
{
    options.DatabaseName = "MongoObjectDatabase";

    builder.RegisterDocumentsExampleWebApi()
        .AddWatchStream()
        .AddRedisLockManager();
});
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
