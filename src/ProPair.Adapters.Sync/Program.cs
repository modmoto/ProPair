using ProPair.Adapters.Sync;

// Separate process for syncing New Recruit into our database (Hangfire comes next).
// For now: "explore" talks to the New Recruit API and dumps what it returns.
if (args is ["explore", var outDir, ..])
{
    var config = new ConfigurationBuilder().AddUserSecrets("propair").AddEnvironmentVariables().Build();
    await Explore.RunAsync(config, outDir);
    return;
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "ProPair Sync");
app.Run();
