using ProPair.Adapters.Mongo;
using ProPair.Adapters.NewRecruit;
using ProPair.Adapters.Rest;
using ProPair.Application;

var builder = WebApplication.CreateBuilder(args);

var mongo = builder.Configuration.GetSection("Mongo").Get<MongoOptions>() ?? new MongoOptions();
builder.Services
    .AddApplication()
    .AddDemoTournaments() // real New Recruit data comes from the sync process (ProPair.Adapters.Sync)
    .AddMongo(mongo)
    .AddMongoSyncQueue(mongo);

var app = builder.Build();

await app.Services.EnsureMongoIndexesAsync();

app.UseDomainErrors();
app.MapStaticAssets(); // serves the fingerprinted Blazor WASM assets
app.MapProPairApi();
app.MapFallbackToFile("index.html");

app.Run();
