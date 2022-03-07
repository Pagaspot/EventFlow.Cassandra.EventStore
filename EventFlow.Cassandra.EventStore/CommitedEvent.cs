using EventFlow.EventStores;

namespace EventFlow.Cassandra.EventStore;

public class CommitedEvent : ICommittedDomainEvent
{
    public string? AggregateId { get; set; }
    public int AggregateSequenceNumber { get; set; }
    public string? AggregateName { get; set; }
    public string? EventType { get; set; }
    public int? EventVersion { get; set; }
    public string? Data { get; set; }
    public string? Metadata { get; set; }
    public DateTime? TimeStamp { get; set; }
}