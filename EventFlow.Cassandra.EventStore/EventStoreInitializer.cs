using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

namespace EventFlow.Cassandra.EventStore;

public static class EventStoreInitializer
{
    public static async Task Initialize<T>(ISession session) where T : CommitedEvent
    {
        MappingConfiguration.Global.Define<EventStoreMappings<T>>();

        await new Table<T>(session).CreateIfNotExistsAsync();
    }
}