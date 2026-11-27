using ProPair.Adapters.Mongo;
using ProPair.Adapters.NewRecruit;
using ProPair.Adapters.Rest;
using ProPair.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddNewRecruit()
    .AddMongo(builder.Configuration.GetSection("Mongo").Get<MongoOptions>() ?? new MongoOptions());

var app = builder.Build();

await app.Services.EnsureMongoIndexesAsync();

app.UseDomainErrors();
app.MapStaticAssets(); // serves the fingerprinted Blazor WASM assets
app.MapProPairApi();
app.MapFallbackToFile("index.html");

app.Run();
