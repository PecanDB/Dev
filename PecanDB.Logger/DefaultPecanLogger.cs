namespace PecanDB.Logger
{
    using Akka.Actor;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public class DefaultPecanLogger : IPecanLogger
    {
        //   private readonly ILoggingAdapter _log = Context.GetLogger();
        private static long TraceCount;

        private readonly string GlobalContext = "_GLOBAL_";
        private string LogFile;

        private Action<Dictionary<string, List<string>>, string, string, TraceType, object[], Exception> Logger;
        public Dictionary<string, List<string>> Loggs = new Dictionary<string, List<string>>();
        public Dictionary<string, string> LoggsCombined = new Dictionary<string, string>();
        private TimeSpan? ThrowAfter;

        private bool WaitForEveryLogToComplete;

        public DefaultPecanLogger(bool waitForEveryLogToComplete, string logFile, Action<Dictionary<string, List<string>>, string, string, TraceType, object[], Exception> logger = null, TimeSpan? throwAfter = null)
        {
            this.CreateLogger(waitForEveryLogToComplete, logFile, logger, throwAfter);
        }

        public DefaultPecanLogger(bool waitForEveryLogToComplete, Action<Dictionary<string, List<string>>, string, string, TraceType, object[], Exception> logger, TimeSpan? throwAfter = null)
        {
            this.CreateLogger(waitForEveryLogToComplete, null, logger, throwAfter);
        }

        public DefaultPecanLogger(bool waitForEveryLogToComplete, TimeSpan? throwAfter = null)
        {
            this.CreateLogger(waitForEveryLogToComplete, null, null, throwAfter);
        }

        public IActorRef LoggerActor { get; set; }

        public ActorSystem LoggerActorSystem { get; set; }

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
            if (!this.Loggs.ContainsKey(this.GlobalContext))
            {
                this.Loggs.Add(this.GlobalContext, new List<string>());
                this.LoggsCombined.Add(this.GlobalContext, "");
            }

            void LogWork()
            {
                //LoggsCombined
                string log = $"{TraceCount} traceType :  {traceType} context : {context} message: {message} otherData : {otherData}  {(ex != null ? $"Exception : {JsonConvert.SerializeObject(ex)}" : "")}";
                this.Loggs[this.GlobalContext].Add(log);
                this.LoggsCombined[this.GlobalContext] += "\n\r<br />" + log;
                if (this.Loggs.ContainsKey(context))
                {
                    this.Loggs[context].Add(log);
                    this.LoggsCombined[context] += "\n\r<br />" + log;
                }
                else
                {
                    this.Loggs.Add(
                        context,
                        new List<string>
                        {
                            log
                        });
                    this.LoggsCombined.Add(context, log);
                }

                this.Logger?.Invoke(this.Loggs, context, message, traceType, otherData, ex);

                if (!string.IsNullOrEmpty(this.LogFile))
                    File.AppendAllLines(
                        this.LogFile,
                        new List<string>
                        {
                            log
                        });
            }

            if (this.WaitForEveryLogToComplete)
            {
                object t = this.LoggerActor.Ask((Action)LogWork).Result;
            }
            else
            {
                this.LoggerActor.Tell((Action)LogWork);
            }
        }

        private void CreateLogger(bool waitForEveryLogToComplete, string logFile, Action<Dictionary<string, List<string>>, string, string, TraceType, object[], Exception> logger, TimeSpan? throwAfter)
        {
            this.Name = this.GetType().Name;

            // Step 1. Create configuration object
            /*
             *
             * nlog setup
               var config = new LoggingConfiguration();

              // Step 2. Create targets and add them to the configuration
              var consoleTarget = new ColoredConsoleTarget();
              config.AddTarget("console", consoleTarget);

              // Step 3. Set target properties
              consoleTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";

              // Step 4. Define rules
              var rule1 = new LoggingRule("*", NLogLevel.Debug, consoleTarget);
              config.LoggingRules.Add(rule1);

              // Step 5. Activate the configuration
              LogManager.Configuration = config;
               */

            this.WaitForEveryLogToComplete = waitForEveryLogToComplete;
            this.Logger = logger;
            this.LogFile = logFile;
            this.ThrowAfter = throwAfter;
            if (!string.IsNullOrEmpty(logFile))
                File.Create(logFile);
            this.LoggerActorSystem = ActorSystem.Create("StorageActorSystem-" + Guid.NewGuid().ToString());
            this.LoggerActor = this.LoggerActorSystem.ActorOf(Props.Create<MultiWriterActor>());

            if (throwAfter != null)
                Task.Delay(throwAfter.Value)
                    .ContinueWith(c => throw new Exception($"Logger exceeded its maximum lifespan. Check the loggs : {this.Loggs}"));
        }

        public class MultiWriterActor : ReceiveActor
        {
            public MultiWriterActor()
            {
                this.Receive<Action>(
                    _ =>
                    {
                        try
                        {
                            _();
                            this.Sender.Tell(true);
                        }
                        catch (Exception e)
                        {
                            this.Sender.Tell(false);
                        }
                        TraceCount++;
                    });
            }
        }
    }
}