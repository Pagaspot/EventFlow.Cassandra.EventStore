using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.Cassandra.EventStore.Tests;

[Category(Categories.Integration)]
public class EventStoreTests : TestSuiteForEventStore
{
    private ICassandraSessionProvider _sessionProvider;

    [OneTimeSetUp]
    public void SetUp()
    {
        _sessionProvider = new TestSessionProvider();
        EventStoreInitializer.Initialize(_sessionProvider.Connect()).GetAwaiter().GetResult();
    }
    
    [OneTimeTearDown]
    public void TearDown()
    {
        var session = _sessionProvider.Connect();
        session.DeleteKeyspaceIfExists(session.Keyspace);
        session.Dispose();
    }
    
    protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
    {
        eventFlowOptions.RegisterServices(x => x.Register(_ => _sessionProvider));
        eventFlowOptions.UseEventStore<CassandraEventStore>();
        return eventFlowOptions.CreateResolver();
    }
}