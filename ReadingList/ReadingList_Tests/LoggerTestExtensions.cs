using Microsoft.Extensions.Logging;
using Moq;

namespace ReadingList_Tests;

public static class LoggerTestExtensions
{
    public static void VerifyLogContains<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        string substring,
        Times? times = null)
    {
        times ??= Times.AtLeastOnce();

        logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == level),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v != null && v.ToString().Contains(substring)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times.Value);
    }
}