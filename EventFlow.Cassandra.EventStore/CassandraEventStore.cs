using Cassandra;
using Cassandra.Data.Linq;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;

namespace EventFlow.Cassandra.EventStore;

public class CassandraEventStore : IEventPersistence
{
    private readonly ISession _session;

    public CassandraEventStore(ICassandraSessionProvider storage)
    {
        _session = storage.Connect();
    }

    public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(
        GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
    {
        CqlQuery<CommitedEvent> query = new Table<CommitedEvent>(_session);
        if (!globalPosition.IsStart)
        {
            query = query
                .Where(x => CqlToken.Create(x.AggregateId) > CqlToken.Create(globalPosition.Value));
        }

        var events = (await query.Take(pageSize).ExecuteAsync()).ToArray();
        var nextPosition = events[^1].AggregateId;

        return new AllCommittedEventsPage(new GlobalPosition(nextPosition), events);
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
        IIdentity id, int fromEventSequenceNumber, CancellationToken cancellationToken)
    {
        var events = await new Table<CommitedEvent>(_session)
            .Where(x => x.AggregateId == id.Value)
            .Where(x => x.AggregateSequenceNumber >= fromEventSequenceNumber)
            .OrderBy(x => x.AggregateSequenceNumber)
            .ExecuteAsync();

        return events.ToArray();
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(
        IIdentity id, IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
    {
        if (serializedEvents.Count == 0)
            return Array.Empty<ICommittedDomainEvent>();

        var eventTable = new Table<CommitedEvent>(_session);

        var batch = new BatchStatement()
            .SetBatchType(BatchType.Logged);

        var result = new List<CommitedEvent>();
        var aggregateName = "";
        
        foreach (var sev in serializedEvents)
        {
            aggregateName = sev.Metadata[MetadataKeys.AggregateName];
            var ev = new CommitedEvent()
            {
                BatchId = Guid.Parse(sev.Metadata[MetadataKeys.BatchId]),
                AggregateName = aggregateName,
                AggregateId = id.Value,
                Data = sev.SerializedData,
                Metadata = sev.SerializedMetadata,
                AggregateSequenceNumber = sev.AggregateSequenceNumber
            };

            result.Add(ev);
            batch.Add(eventTable.Insert(ev).IfNotExists());
        }
        
        var rowSet = await _session.ExecuteAsync(batch);

        foreach (var row in rowSet)
        {
            if (row != null && row.GetColumn("[applied]") != null && !row.GetValue<bool>("[applied]"))
            {
                var version = row.GetValue<int>(nameof(CommitedEvent.AggregateSequenceNumber).ToLower());
                throw new OptimisticConcurrencyException(
                    $"Aggregate {aggregateName} sequence number '{version}' already exists for {id.Value}");
            }
        }

        return result;
    }

    public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
    {
        await new Table<CommitedEvent>(_session)
            .Where(x => x.AggregateId == id.Value)
            .Delete()
            .ExecuteAsync();
    }
}