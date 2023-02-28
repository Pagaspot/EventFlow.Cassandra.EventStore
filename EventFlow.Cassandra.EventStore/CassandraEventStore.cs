using Cassandra;
using Cassandra.Data.Linq;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;

namespace EventFlow.Cassandra.EventStore;

public class CassandraEventStore<T> : IEventPersistence where T : CommitedEvent, new()
{
    private readonly ISession _session;
    private readonly BatchType _batchType;
    private readonly ICommitedEventFactory<T>? _commitedEventFactory;

    public CassandraEventStore(
        ICassandraSessionProvider storage,
        ICommitedEventFactory<T>? commitedEventFactory)
    {
        _session = storage.Connect();
        _batchType = storage.DefaultBatchType;
        _commitedEventFactory = commitedEventFactory;
    }

    public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(
        GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
    {
        CqlQuery<T> query = new Table<T>(_session);
        if (!globalPosition.IsStart)
        {
            query = query.Where(x => CqlToken.Create(x.AggregateId) > CqlToken.Create(globalPosition.Value));
        }

        var events = (await query.Take(pageSize).ExecuteAsync()).ToArray();
        var nextPosition = events[^1].AggregateId;

        return new AllCommittedEventsPage(new GlobalPosition(nextPosition), events);
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
        IIdentity id, int fromEventSequenceNumber, CancellationToken cancellationToken)
    {
        var events = await new Table<T>(_session)
            .Where(x => x.AggregateId == id.Value)
            .Where(x => x.AggregateSequenceNumber >= fromEventSequenceNumber)
            .OrderBy(x => x.AggregateSequenceNumber)
            .ExecuteAsync();

        return events.ToArray();
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(
        IIdentity id, 
        IReadOnlyCollection<SerializedEvent> serializedEvents, 
        CancellationToken cancellationToken)
    {
        if (serializedEvents.Count == 0)
            return Array.Empty<ICommittedDomainEvent>();

        var eventTable = new Table<T>(_session);

        var batch = new BatchStatement().SetBatchType(_batchType);

        var result = new List<T>();
        var aggregateName = "";
        
        foreach (var sev in serializedEvents)
        {
            var ev = new T
            {
                AggregateId = id.Value,
                AggregateSequenceNumber = sev.AggregateSequenceNumber,
                AggregateName = sev.Metadata[MetadataKeys.AggregateName],
                EventType = sev.Metadata.EventName,
                EventVersion = sev.Metadata.EventVersion,
                Data = sev.SerializedData,
                Metadata = sev.SerializedMetadata,
                TimeStamp = sev.Metadata.Timestamp.UtcDateTime,
            };
            
            if (_commitedEventFactory != null)
            {
                _commitedEventFactory.Augment(ev, id, sev);
            }
            
            aggregateName = ev.AggregateName;

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
        await new Table<T>(_session)
            .Where(x => x.AggregateId == id.Value)
            .Delete()
            .ExecuteAsync();
    }
}