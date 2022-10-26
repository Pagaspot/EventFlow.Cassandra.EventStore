using EventFlow.Core;
using EventFlow.EventStores;

namespace EventFlow.Cassandra.EventStore;

public interface ICommitedEventFactory<in T> where T : CommitedEvent, new()
{
    void Augment(T ev, IIdentity id, SerializedEvent sev);
}