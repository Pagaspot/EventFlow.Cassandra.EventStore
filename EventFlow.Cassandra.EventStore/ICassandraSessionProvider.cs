using Cassandra;

namespace EventFlow.Cassandra.EventStore;

public interface ICassandraSessionProvider
{
    ISession Connect();
}