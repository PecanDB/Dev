namespace PecanDB
{
    using Akka.Actor;
    using Akka.Dispatch;
    using Akka.Event;
    using System;

    /// <summary>
    ///     This class is used to receive log events and sends them to
    ///     the configured NLog logger. The following log events are
    ///     recognized: <see cref="Akka.Event.Debug" />, <see cref="Info" />,
    ///     <see cref="Warning" /> and <see cref="Error" />.
    /// </summary>
    public class PecanDbLogger : ReceiveActor, IRequiresMessageQueue<ILoggerMessageQueueSemantics>
    {
        public static IPecanLogger DefaultPecanLogger;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        /// <summary>
        ///     Initializes a new instance of the <see cref="PecanDbLogger" /> class.
        /// </summary>
        public PecanDbLogger()
        {
            this.Receive<Error>(m => Log(TraceType.Error, (logger, logSource) => LogEvent(logger, TraceType.Error, logSource, m.Cause, "{0}", m.Message)));
            this.Receive<Warning>(m => Log(TraceType.Warn, (logger, logSource) => LogEvent(logger, TraceType.Warn, logSource, "{0}", m.Message)));
            this.Receive<Info>(m => Log(TraceType.Info, (logger, logSource) => LogEvent(logger, TraceType.Info, logSource, "{0}", m.Message)));
            this.Receive<Debug>(m => Log(TraceType.Debug, (logger, logSource) => LogEvent(logger, TraceType.Debug, logSource, "{0}", m.Message)));
            this.Receive<InitializeLogger>(
                m =>
                {
                    this._log.Info("NLogLogger started");
                    this.Sender.Tell(new LoggerInitialized());
                });
        }

        private static void Log(TraceType logEvent, Action<IPecanLogger, string> logStatement)
        {
            if (DefaultPecanLogger != null)
                logStatement(DefaultPecanLogger, logEvent.ToString());
        }

        private static void LogEvent(IPecanLogger logger, TraceType level, string logSource, string message, params object[] parameters)
        {
            LogEvent(logger, level, logSource, null, message, parameters);
        }

        private static void LogEvent(IPecanLogger logger, TraceType level, string logSource, Exception exception, string message, params object[] parameters)
        {
            logger.Trace(logger.Name + " > " + logSource, message + " > " + exception?.Message + " > " + exception?.InnerException?.Message, null, level, parameters);
        }
    }
}