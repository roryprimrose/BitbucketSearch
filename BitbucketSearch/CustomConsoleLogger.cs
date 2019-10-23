namespace BitbucketSearch
{
    using Microsoft.Extensions.Logging;
    using System;

    public class CustomConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private string _messageFormat;
        private LogLevel _logLevel;

        public CustomConsoleLogger(string categoryName, LogLevel logLevel, string messageFormat)
        {
            _categoryName = categoryName;
            _logLevel = logLevel;
            _messageFormat = messageFormat;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // Need to run a hacky runtime string interpolation
            var message = _messageFormat;

            message = message.Replace("{formatter(state, exception)}", formatter(state, exception));
            message = message.Replace("{category}", _categoryName);
            message = message.Replace("{logLevel}", logLevel.ToString());
            message = message.Replace("{eventId.Id}", eventId.Id.ToString());
            message = message.Replace("{eventId}", eventId.ToString());
            message = message.Replace("{state}", state.ToString());

            if (exception != null) 
            {
                message = message.Replace("{exception}", exception.ToString());
            }

            Console.WriteLine(message);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel <= _logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}