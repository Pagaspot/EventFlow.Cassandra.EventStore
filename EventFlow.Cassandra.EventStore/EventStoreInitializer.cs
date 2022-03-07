using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

namespace EventFlow.Cassandra.EventStore;

public static class EventStoreInitializer
{
    public static async Task Initialize(ISession session)
    {
        MappingConfiguration.Global.Define<EventStoreMappings>();

        try
        {
            session.Execute("ALTER TABLE events ADD eventversion int;");
            session.Execute("ALTER TABLE events ADD eventtype text;");
            session.Execute("ALTER TABLE events ADD timestamp timestamp;");
            session.Execute("ALTER TABLE events DROP batchid;");
        }
        catch (Exception)
        {
            // Ignore
        }

        await new Table<CommitedEvent>(session).CreateIfNotExistsAsync();
    }
}