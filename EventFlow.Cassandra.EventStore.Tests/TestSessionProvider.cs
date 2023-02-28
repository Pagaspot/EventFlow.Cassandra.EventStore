using Cassandra;

namespace EventFlow.Cassandra.EventStore.Tests;

public class TestSessionProvider : ICassandraSessionProvider
{
    private const string Keyspace = "events_tests"; 
    private readonly Cluster _cluster;
    
    public TestSessionProvider()
    {
        _cluster = Cluster.Builder()
            .AddContactPoints("localhost")
            .Build();

        _cluster.Connect().CreateKeyspaceIfNotExists(Keyspace);
    }
    
    public ISession Connect()
    {
        return _cluster.Connect(Keyspace);
    }

    public BatchType DefaultBatchType => BatchType.Logged;
}