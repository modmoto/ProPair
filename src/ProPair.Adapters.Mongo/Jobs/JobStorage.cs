using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using MongoDB.Driver;
using ProPair.Application.Ports;
using ProPair.Application.UseCases;

namespace ProPair.Adapters.Mongo.Jobs;

/// <summary>Hangfire jobs live in our Mongo database (collections prefixed "hangfire"). API and sync process share it.</summary>
public static class ProPairJobStorage
{
    public static MongoStorage Create(MongoOptions options) =>
        new(new MongoClient(options.ConnectionString), options.Database, new MongoStorageOptions
        {
            Prefix = "hangfire",
            MigrationOptions = new MongoMigrationOptions
            {
                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                BackupStrategy = new CollectionMongoBackupStrategy(),
            },
            // works on a single mongod too (change streams need a replica set)
            CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection,
        });
}

/// <summary>Enqueues work for the sync process. The API process never runs jobs itself.</summary>
internal sealed class HangfireSyncQueue(IBackgroundJobClient jobs) : ISyncQueue
{
    public void RequestTournamentSync(string externalId) =>
        jobs.Enqueue<TournamentSyncJob>(j => j.SyncOneAsync(externalId));
}
