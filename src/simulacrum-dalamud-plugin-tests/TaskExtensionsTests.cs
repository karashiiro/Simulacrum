using Dalamud.Plugin.Services;
using Moq;

namespace Simulacrum.Tests;

public class TaskExtensionsTests
{
    [Fact]
    public async Task FireAndForget_WhenFailed_LogsError()
    {
        var exception = new InvalidOperationException();

        var logger = new Mock<IPluginLog>();
        logger.Setup(l => l.Error(exception, It.IsAny<string>()))
            .Verifiable(Times.Once, "The thrown exception was never logged.");

        // Arrange: Set up a task that throws an exception
        var task = Task.Factory.StartNew(() => throw exception);

        // Act: Fire and forget it
        task.FireAndForget(logger.Object);

        // Await the task so we know it's complete
        try
        {
            await task;
        }
        catch (Exception)
        {
            /* ignored */
        }

        // Assert: The exception was logged
        logger.Verify();
    }
}