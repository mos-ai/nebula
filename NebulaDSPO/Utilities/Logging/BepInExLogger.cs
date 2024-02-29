using Microsoft.Extensions.Logging;

namespace NebulaDSPO.Utilities.Logging;

public class BepInExLogger : ILogger
{
    private readonly string _categoryName;

    public BepInExLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Implement your logic here (if needed)
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        // Map log levels and delegate to BepInEx's logger
        switch (logLevel)
        {
            case LogLevel.Information:
                NebulaModel.Logger.Log.Info($"[{_categoryName}] {formatter(state, exception)}");
                break;
            case LogLevel.Warning:
                NebulaModel.Logger.Log.Warn($"[{_categoryName}] {formatter(state, exception)}");
                break;
            case LogLevel.Error:
                NebulaModel.Logger.Log.Error($"[{_categoryName}] {formatter(state, exception)}");
                break;
                // Handle other log levels as needed
        }
    }
}
