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
// serves the Blazor WASM assets and index.html for all page routes (StaticWebAssetSpaFallbackEnabled
// in ProPair.Web) – no MapFallbackToFile on top: two fallbacks are ambiguous (500)
app.MapStaticAssets();
app.MapProPairApi();

app.Run();
