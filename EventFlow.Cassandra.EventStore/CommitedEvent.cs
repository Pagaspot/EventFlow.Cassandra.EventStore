using EventFlow.EventStores;

namespace EventFlow.Cassandra.EventStore;

public class CommitedEvent : ICommittedDomainEvent
{
    public Guid BatchId { get; set; }
    public string? AggregateName { get; set; }
    public string? AggregateId { get; set; }
    public string? Data { get; set; }
    public string? Metadata { get; set; }
    public int AggregateSequenceNumber { get; set; }
}