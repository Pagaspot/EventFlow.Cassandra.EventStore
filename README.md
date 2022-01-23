
# EventFlow Cassandra event store driver

<a href="https://www.nuget.org/packages/Pagaspot.EventFlow.Cassandra.EventStore/"><img src="https://img.shields.io/nuget/v/Pagaspot.EventFlow.Cassandra.EventStore.svg" /></a>

1. Implement Cassandra session provider interface and register it in IoC container:
```csharp
public interface ICassandraSessionProvider
{
    ISession Connect();
}
```
It should setup a cluster and call `_cluster.Connect(keyspaceName)` to create a session.
You can select any keyspace name you like.  
More details: [DataStax Cassandra Driver](https://github.com/datastax/csharp-driver#basic-usage)

2. Register `CassandraEventStore`:
```csharp
services.AddEventFlow(ef =>
{
    ef.UseEventStore<CassandraEventStore>();
    // ...
}
```

3. Initialize mappings and ensure the table exists by running:
```csharp
ISession session = // ...
await EventStoreInitializer.Initialize(session);
```
Before any command is issued (e.g. in `Startup.Configure()`): 
