namespace CLIHub.Core.Tests.Events;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using CLIHub.Core.Events;
using CLIHub.Core.Infrastructure.Logging;

public class ProjectSelectionLoggingSubscriberTests
{
    [Fact]
    public void Constructor_SubscribesToProjectSelected()
    {
        var mockSubscriber = new Mock<IDomainEventSubscriber>();
        var mockContext = new Mock<ILoggingContextProvider>();

        mockSubscriber
            .Setup(s => s.Subscribe(It.IsAny<Action<ProjectSelected>>()))
            .Returns(Mock.Of<IDisposable>());

        _ = new ProjectSelectionLoggingSubscriber(
            mockSubscriber.Object,
            mockContext.Object,
            NullLogger<ProjectSelectionLoggingSubscriber>.Instance);

        mockSubscriber.Verify(s => s.Subscribe(It.IsAny<Action<ProjectSelected>>()), Times.Once);
    }

    [Fact]
    public void OnProjectSelected_LogsEventWithCorrelationScope()
    {
        var mockSubscriber = new Mock<IDomainEventSubscriber>();
        var mockContext = new Mock<ILoggingContextProvider>();
        var mockLogger = new Mock<ILogger<ProjectSelectionLoggingSubscriber>>();
        var mockScope = new Mock<IDisposable>();

        Action<ProjectSelected>? capturedHandler = null;
        mockSubscriber
            .Setup(s => s.Subscribe(It.IsAny<Action<ProjectSelected>>()))
            .Callback<Action<ProjectSelected>>(handler => capturedHandler = handler)
            .Returns(Mock.Of<IDisposable>());

        mockContext
            .Setup(c => c.BeginOperation("ProjectSelected"))
            .Returns(mockScope.Object);

        _ = new ProjectSelectionLoggingSubscriber(
            mockSubscriber.Object,
            mockContext.Object,
            mockLogger.Object);

        capturedHandler?.Invoke(new ProjectSelected("proj-1", @"C:\Projects\Test", DateTimeOffset.UtcNow));

        mockContext.Verify(c => c.BeginOperation("ProjectSelected"), Times.Once);
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        mockScope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        var mockSubscriber = new Mock<IDomainEventSubscriber>();
        var mockContext = new Mock<ILoggingContextProvider>();
        var mockSubscription = new Mock<IDisposable>();

        mockSubscriber
            .Setup(s => s.Subscribe(It.IsAny<Action<ProjectSelected>>()))
            .Returns(mockSubscription.Object);

        var subscriber = new ProjectSelectionLoggingSubscriber(
            mockSubscriber.Object,
            mockContext.Object,
            NullLogger<ProjectSelectionLoggingSubscriber>.Instance);

        subscriber.Dispose();

        mockSubscription.Verify(s => s.Dispose(), Times.Once);
    }
}
