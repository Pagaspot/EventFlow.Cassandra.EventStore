using Cassandra.Mapping;

namespace EventFlow.Cassandra.EventStore;

public class EventStoreMappings : Mappings
{
    public EventStoreMappings()
    {
        For<CommitedEvent>()
            .TableName("events")
            .PartitionKey(x => x.AggregateId)
            .ClusteringKey(x => x.AggregateSequenceNumber, SortOrder.Ascending);
    }
}