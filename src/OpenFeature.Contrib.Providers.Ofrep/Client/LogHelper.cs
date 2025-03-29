using Microsoft.Extensions.Logging;

namespace OpenFeature.Contrib.Providers.Ofrep.Client
{
    /// <summary>
    /// Provides ILogger instances for internal development debugging purposes only.
    /// In Release builds, it provides NullLogger instances which do nothing.
    /// </summary>
    internal static class DevLoggerProvider
    {
#if DEBUG
        // In DEBUG builds, create a real LoggerFactory
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug); // Capture Debug level and above
        });

        public static ILogger<T> CreateLogger<T>() => _loggerFactory.CreateLogger<T>();
        public static ILogger CreateLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);
#else
        // In RELEASE builds, provide NullLogger instances which perform no operations.
        public static ILogger<T> CreateLogger<T>() => Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
        public static ILogger CreateLogger(string categoryName) => Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
#endif
    }
}