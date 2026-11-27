using Hangfire;
using ProPair.Adapters.Mongo;
using ProPair.Adapters.NewRecruit;
using ProPair.Adapters.NewRecruit.Api;
using ProPair.Adapters.Sync;
using ProPair.Application;
using ProPair.Application.UseCases;

// Separate process: pulls New Recruit into our database with Hangfire. The API only enqueues.
// "explore" / "explore-call" talk to the New Recruit API directly and dump raw JSON (for checking the format).
if (args is ["explore-call", var callDir, var path, var body])
{
    var config = new ConfigurationBuilder().AddUserSecrets("propair").AddEnvironmentVariables().Build();
    await Explore.CallOnceAsync(config, callDir, path, body);
    return;
}

if (args is ["explore", var outDir, ..])
{
    var config = new ConfigurationBuilder().AddUserSecrets("propair").AddEnvironmentVariables().Build();
    await Explore.RunAsync(config, outDir);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets("propair");

var mongo = builder.Configuration.GetSection("Mongo").Get<MongoOptions>() ?? new MongoOptions();
builder.Services
    .AddApplication()
    .AddNewRecruit(builder.Configuration.GetSection("NewRecruit").Get<NewRecruitOptions>() ?? new NewRecruitOptions())
    .AddMongo(mongo)
    .AddMongoSyncQueue(mongo); // job storage shared with the API

builder.Services.AddHangfire((sp, c) => c.UseStorage(sp.GetRequiredService<JobStorage>()));
// one worker: New Recruit requests stay sequential
builder.Services.AddHangfireServer(o => o.WorkerCount = 1);

var app = builder.Build();
await app.Services.EnsureMongoIndexesAsync();

// daily: everything from today until 12 months ahead (04:00 UTC)
app.Services.GetRequiredService<IRecurringJobManager>()
    .AddOrUpdate<TournamentSyncJob>("sync-upcoming", j => j.SyncUpcomingAsync(), Cron.Daily(4));

// Dashboard: local requests only (Hangfire default) – an admin UI with login comes later.
app.MapHangfireDashboard("/hangfire");

app.MapGet("/", () => Results.Text("""
    ProPair Sync
    POST /sync/upcoming                        – sync now (today … +12 months)
    POST /sync/tournaments/{newRecruitId}      – one tournament
    POST /sync/backfill?from=yyyy-MM-dd&to=…   – also finished tournaments (testing)
    GET  /hangfire                             – jobs
    """));

app.MapPost("/sync/upcoming", (IBackgroundJobClient jobs) =>
    Results.Accepted(value: jobs.Enqueue<TournamentSyncJob>(j => j.SyncUpcomingAsync())));

app.MapPost("/sync/tournaments/{externalId}", (string externalId, IBackgroundJobClient jobs) =>
    Results.Accepted(value: jobs.Enqueue<TournamentSyncJob>(j => j.SyncOneAsync(externalId))));

app.MapPost("/sync/backfill", (DateOnly from, DateOnly to, IBackgroundJobClient jobs) =>
    Results.Accepted(value: jobs.Enqueue<TournamentSyncJob>(j =>
        j.BackfillAsync(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd")))));

app.Run();
