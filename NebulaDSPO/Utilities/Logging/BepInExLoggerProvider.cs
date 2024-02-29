using Microsoft.Extensions.Logging;

namespace NebulaDSPO.Utilities.Logging;

public class BepInExLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new BepInExLogger(categoryName);
    }

    public void Dispose()
    {
        // Not used.
    }
}
