namespace PecanDB
{
    using System;

    public class DataBaseParam
    {
        public string FinalExportDir;
        public string FinalBackUpDir;
        public string DbFileStorageLocationName;

        public string BackUpDir { get; set; }

        public string ExportDir { get; set; }

        public string DbStorageLocation { get; set; }

        public string DestinationFilesStorage { get; set; }

        public bool UseInMemoryStorage { get; set; }

        public string DataBaseName { get; set; }

        public DateTime DataBaseStartedAt { get; set; }

        public string BackUpDirName { get; set; }

        public string ExportDirName { get; set; }

        public string DestinationFilesStorageName { get; set; }
    }
}