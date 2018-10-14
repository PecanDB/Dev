namespace PecanDB.Logger.NLog
{
    using global::NLog;
    using System;

    public class NLogPecanLogger : IPecanLogger
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void Trace(string context, string message, Exception ex = null, params object[] otherData)
        {
            this.Log(context, message, TraceType.Trace, ex, otherData);
        }

        public void Debug(string context, string message, Exception ex = null, params object[] otherData)
        {
            this.Log(context, message, TraceType.Debug, ex, otherData);
        }

        public void Error(string context, string message, Exception ex = null, params object[] otherData)
        {
            this.Log(context, message, TraceType.Error, ex, otherData);
        }

        public void Fatal(string context, string message, Exception ex = null, params object[] otherData)
        {
            this.Log(context, message, TraceType.Fatal, ex, otherData);
        }

        public string Name { get; set; }

        public void Log(string context, string message, TraceType traceType, Exception ex = null, params object[] otherData)
        {
            LogLevel logLevel = LogLevel.Debug;
            switch (traceType)
            {
                case TraceType.Trace:
                    logLevel = LogLevel.Trace;
                    break;

                case TraceType.Error:
                    logLevel = LogLevel.Error;
                    break;

                case TraceType.Warn:
                    logLevel = LogLevel.Warn;
                    break;

                case TraceType.Fatal:
                    logLevel = LogLevel.Fatal;
                    break;

                case TraceType.Info:
                    logLevel = LogLevel.Info;
                    break;

                case TraceType.Debug:
                    logLevel = LogLevel.Debug;
                    break;
            }
            logger.Log(logLevel, ex, $"{context} : {message}", otherData);
        }
    }
}