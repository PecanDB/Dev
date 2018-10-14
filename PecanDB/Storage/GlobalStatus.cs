namespace PecanDb.Storage
{
    using System;

    internal class GlobalStatus
    {
        internal static volatile bool HasStaleResults;

        internal static DateTime LastWriteUtc;
    }
}