using Hangfire;
using Hangfire.Dashboard;
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
// dashboard only – jobs run in the sync process (no AddHangfireServer here)
builder.Services.AddHangfire((sp, c) => c.UseStorage(sp.GetRequiredService<JobStorage>()));

var app = builder.Build();

await app.Services.EnsureMongoIndexesAsync();

app.UseDomainErrors();
// serves the Blazor WASM assets and index.html for all page routes (StaticWebAssetSpaFallbackEnabled
// in ProPair.Web) – no MapFallbackToFile on top: two fallbacks are ambiguous (500)
app.MapStaticAssets();
app.MapProPairApi();
// TODO: public while we are testing – needs admin login before going live
app.MapHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [new AllowAll()] });

app.Run();

internal sealed class AllowAll : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
