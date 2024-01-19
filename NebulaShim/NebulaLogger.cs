using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NebulaShim;
internal class NebulaLogger : ILogger
{
    private string _scope = string.Empty;
    private readonly NebulaModel.Logger.ILogger _logger;

    public NebulaLogger(NebulaModel.Logger.ILogger logger, string? scope = null)
    {
        _logger = logger;
        if (scope is not null)
        {
            _scope = scope;
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        _scope = state.GetType().Name;
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                _logger.LogDebug($"[{_scope}]: {formatter.Invoke(state, exception)}");
                break;
            case LogLevel.Debug:
                _logger.LogDebug($"[{_scope}]: {formatter.Invoke(state, exception)}");
                break;
            case LogLevel.Information:
                _logger.LogInfo($"[{_scope}]: {formatter.Invoke(state, exception)}");
                break;
            case LogLevel.Warning:
                _logger.LogWarning($"[{_scope}]: {formatter.Invoke(state, exception)}");
                break;
            case LogLevel.Error:
                _logger.LogError($"[{_scope}]: {formatter.Invoke(state, exception)}");
                break;
            case LogLevel.Critical:
                _logger.LogError($"[{_scope}]: {formatter.Invoke(state, exception)}");
                break;
            case LogLevel.None:
                break;
            default:
                break;
        }
    }
}
