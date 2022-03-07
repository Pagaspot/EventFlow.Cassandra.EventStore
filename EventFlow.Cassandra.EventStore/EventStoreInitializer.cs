using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

namespace EventFlow.Cassandra.EventStore;

public static class EventStoreInitializer
{
    public static async Task Initialize(ISession session)
    {
        MappingConfiguration.Global.Define<EventStoreMappings>();

        await new Table<CommitedEvent>(session).CreateIfNotExistsAsync();
    }
}