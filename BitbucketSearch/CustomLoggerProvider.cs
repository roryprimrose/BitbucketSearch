namespace BitbucketSearch
{
    using Microsoft.Extensions.Logging;

    public class CustomLoggerProvider : ILoggerProvider
    {
        private const string DefaultMessageFormat = "{logLevel}: {category}[{eventId.Id}]: {formatter(state, exception)}";
        private string _messageFormat = DefaultMessageFormat;
        private LogLevel _logLevel = LogLevel.Information;

        public CustomLoggerProvider() 
        {
        }

        public CustomLoggerProvider(LogLevel logLevel) 
        {
            _logLevel = logLevel;
        }

        public CustomLoggerProvider(string messageFormat) 
        {
            _messageFormat = messageFormat;
        }

        public CustomLoggerProvider(LogLevel logLevel, string messageFormat) 
        {
            _logLevel = logLevel;
            _messageFormat = messageFormat;
        }

        public void Dispose() { }

        public ILogger CreateLogger(string categoryName)
        {
            return new CustomConsoleLogger(categoryName, _logLevel, _messageFormat);
        }
    }
}