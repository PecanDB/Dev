namespace PecanDB
{
    using System;

    public interface IPecanLogger
    {
        string Name { get; set; }

        void Log(string context, string message, TraceType traceType, Exception ex = null, params object[] otherData);

        void Trace(string context, string message, Exception ex = null, params object[] otherData);

        void Fatal(string context, string message, Exception ex = null, params object[] otherData);

        void Debug(string context, string message, Exception ex = null, params object[] otherData);

        void Error(string context, string message, Exception ex = null, params object[] otherData);
    }
}