using Cassandra.Mapping;

namespace EventFlow.Cassandra.EventStore;

public class EventStoreMappings<T> : Mappings where T : CommitedEvent
{
    public EventStoreMappings()
    {
        For<T>()
            .TableName("events")
            .PartitionKey(x => x.AggregateId)
            .ClusteringKey(x => x.AggregateSequenceNumber, SortOrder.Ascending);
    }
}