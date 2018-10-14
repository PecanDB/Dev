namespace PecanDB
{
    using PecanDb.Storage.StorageSystems;
    using System;

    public class DatabaseOptions
    {
        public DatabaseOptions(bool enableCaching = false, bool enableFasterCachingButWithLeakyUpdates = false)
        {
            this.EnableCaching = enableCaching;
            this.EnableFasterCachingButWithLeakyUpdates = enableFasterCachingButWithLeakyUpdates;
        }

        public IPecanLogger Logger { set; get; }

        public IStorageIO StorageIO { set; get; }

        /// <summary>
        ///     Faster gets but slower saves when true, but opposite when false
        /// </summary>
        public bool EnableCaching { get; set; }

        public bool EnableFasterCachingButWithLeakyUpdates { get; set; }

        public TimeSpan MaxResponseTime { get; set; }

        public bool PrettifyDocuments { get; set; }

        internal bool UseSingleFile { get; set; }

        public bool DontWaitForWrites { get; set; }

        public bool DontWaitForWritesOnCreate { get; set; }

        public string StorageDirectory { get; set; }

        public string StorageName { get; set; }

        public string RunAsServerWithAddress { get; set; }

        public string UseRemoteServerAdrressForClientRequests { get; set; }

        public bool UseInMemoryStorage { get; set; }
    }
}