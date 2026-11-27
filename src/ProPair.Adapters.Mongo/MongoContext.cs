using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using ProPair.Adapters.Mongo.Documents;

namespace ProPair.Adapters.Mongo;

public sealed class MongoOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "propair";
}

internal sealed class MongoContext
{
    static MongoContext()
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.TryRegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
    }

    public MongoContext(MongoOptions options)
    {
        var db = new MongoClient(options.ConnectionString).GetDatabase(options.Database);
        Teams = db.GetCollection<TeamDoc>("teams");
        Tournaments = db.GetCollection<TournamentDoc>("tournaments");
        Preparations = db.GetCollection<PreparationDoc>("preparations");
        Matrices = db.GetCollection<MatrixDoc>("matrices");
    }

    public IMongoCollection<TeamDoc> Teams { get; }
    public IMongoCollection<TournamentDoc> Tournaments { get; }
    public IMongoCollection<PreparationDoc> Preparations { get; }
    public IMongoCollection<MatrixDoc> Matrices { get; }

    public async Task EnsureIndexesAsync()
    {
        await Tournaments.Indexes.CreateOneAsync(new CreateIndexModel<TournamentDoc>(
            Builders<TournamentDoc>.IndexKeys.Ascending(d => d.ExternalId), new() { Unique = true }));
        await Preparations.Indexes.CreateManyAsync([
            new(Builders<PreparationDoc>.IndexKeys.Ascending(d => d.EditToken), new() { Unique = true }),
            new(Builders<PreparationDoc>.IndexKeys.Ascending(d => d.ReadToken), new() { Unique = true }),
            new(Builders<PreparationDoc>.IndexKeys.Ascending(d => d.TeamId).Ascending(d => d.TournamentId),
                new() { Unique = true }),
        ]);
        await Matrices.Indexes.CreateOneAsync(new CreateIndexModel<MatrixDoc>(
            Builders<MatrixDoc>.IndexKeys.Ascending(d => d.PreparationId).Ascending(d => d.OpponentParticipantId),
            new() { Unique = true }));
        await Teams.Indexes.CreateOneAsync(new CreateIndexModel<TeamDoc>(
            Builders<TeamDoc>.IndexKeys.Ascending(d => d.Name), new() { Unique = true }));
    }
}
