namespace PecanDB
{
    using PecanDb.Storage;
    using PecanDb.Storage.StorageSystems;
    using PecanDB.Remoting;
    using System;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    ///     Only create one instance of this per application
    /// </summary>
    public class PecanDocumentStore : IDocumentStore
    {
        private string DataBaseName;
        private DatabaseService databaseService;
        private DateTime DataBaseStartedAt;

        private bool UseInMemoryStorage;

        /// <summary>
        ///     Document store should only have one instance per application
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="logger"></param>
        /// <param name="databaseOptions"></param>
        public PecanDocumentStore(string databaseName, DatabaseOptions databaseOptions = null)
        {
            databaseOptions = databaseOptions ?? new DatabaseOptions();
            this.Logger = databaseOptions.Logger;
            //DatabaseOptions = databaseOptions;
            this.CreateDB(databaseName, databaseOptions, databaseOptions.UseSingleFile);
        }

        /// <summary>
        ///     Document store should only have one instance per application
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="databaseOptions"></param>
        public PecanDocumentStore(string databaseName, bool useInMemory, DatabaseOptions databaseOptions = null)
        {
            databaseOptions = databaseOptions ?? new DatabaseOptions();
            databaseOptions.UseInMemoryStorage = useInMemory;
            this.CreateDB(databaseName, databaseOptions, databaseOptions?.UseSingleFile ?? false);
        }

        private DatabaseOptions DatabaseOptions { set; get; }

        public IPecanLogger Logger { get; set; }

        public ISession OpenSession()
        {
            var session = new Session(this.databaseService, this.Logger, RemoteServerAdrress);

            return session;
        }

        public DataBaseParam InitializeAndGetDataBaseParams(string destinationDir)
        {
            string backUpDir = "PecanDBBackUp";
            string exportDir = "PecanDBExport";
            if (destinationDir != null)
            {
                destinationDir = destinationDir.TrimEnd('/').TrimEnd('\\').TrimEnd('/').TrimEnd('\\');
            }
            var finalBackUpDir = (string.IsNullOrEmpty(destinationDir) ? AppDomain.CurrentDomain.BaseDirectory : destinationDir) + "\\" + backUpDir;
            var finalExportDir = (string.IsNullOrEmpty(destinationDir) ? AppDomain.CurrentDomain.BaseDirectory : destinationDir) + "\\" + exportDir;

            var dbStorageLocationName = Path.GetFileName((DataBaseSettings.DbStorageLocation));
            var dbFileStorageLocationName = Path.GetFileName((Session.DestinationFilesStorage));

            var param = new DataBaseParam
            {
                BackUpDir = finalBackUpDir,
                ExportDir = finalExportDir + "\\" + dbStorageLocationName,
                BackUpDirName = backUpDir,
                ExportDirName = exportDir,
                DbStorageLocation = DataBaseSettings.DbStorageLocation,
                DestinationFilesStorage = Session.DestinationFilesStorage,
                DestinationFilesStorageName = dbFileStorageLocationName,
                UseInMemoryStorage = this.UseInMemoryStorage,
                DataBaseName = this.DataBaseName,
                DataBaseStartedAt = this.DataBaseStartedAt,
                FinalBackUpDir = finalBackUpDir,
                FinalExportDir = finalExportDir,
                DbFileStorageLocationName = dbFileStorageLocationName
            };

            CleanDirectoryForBackUp(param);

            return param;
        }

        public void CleanDirectoryForBackUp(DataBaseParam param)
        {
            if (!this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.FinalExportDir))
                this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.FinalExportDir);

            if (!this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.FinalBackUpDir))
                this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.FinalBackUpDir);

            if (!this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.FinalExportDir + "\\" + param.DbFileStorageLocationName))
                this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.FinalExportDir + "\\" + param.DbFileStorageLocationName);

            if (this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.ExportDir))
                DeleteDirectory(param.ExportDir);

            if (!this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.ExportDir))
                this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.ExportDir);

            if (!this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.BackUpDir))
                this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.BackUpDir);

            if (!this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.DbStorageLocation))
                this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.DbStorageLocation);

            if (!this.databaseService.DataBaseSettings.FileIo.DirectoryExists(param.DestinationFilesStorage))
                this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.DestinationFilesStorage);

            this.databaseService.DataBaseSettings.FileIo.CreateDirectory(param.ExportDir);
        }

        public bool DeleteAllDatabases(bool backUpbeforeDelete = true, string destinationDir = null)
        {
            if (backUpbeforeDelete)
            {
                if (!ExportAllDatabases(destinationDir))
                {
                    throw new Exception("Unable to export databases before deleting them");
                }
            }
            DataBaseParam param = this.InitializeAndGetDataBaseParams(destinationDir);
            DeleteDirectory(param.DbStorageLocation);
            return true;
        }

        public static void DeleteDirectory(string path)
        {
            var name = "Deleted" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            var di = new DirectoryInfo(path);
            if (di == null)
            {
                throw new ArgumentNullException("di", "Directory info to rename cannot be null");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("New name cannot be null or blank", "name");
            }

            di.MoveTo(Path.Combine(di.Parent.FullName, name));
        }

        public bool ExportAllDatabases(string destinationDir = null)
        {
            DataBaseParam param = this.InitializeAndGetDataBaseParams(destinationDir);
            CleanDirectoryForBackUp(param);

            CopyDir.Copy(param.DbStorageLocation, param.ExportDir);
            CopyDir.Copy(param.DestinationFilesStorage, param.ExportDir + "\\" + param.DestinationFilesStorageName);

            string dest = param.BackUpDir + "\\" + param.ExportDirName + "-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".zip";
            ZipFile.CreateFromDirectory(param.ExportDir, dest);

            return true;
        }

        public void Dispose()
        {
            this.databaseService.Dispose();
        }

        private RemoteAccess RemoteAccess { set; get; }

        private void CreateDB(string databaseName, DatabaseOptions databaseOptions, bool useSingleFile)
        {
            this.DataBaseStartedAt = DateTime.UtcNow;
            this.DataBaseName = databaseName;
            this.DatabaseOptions = databaseOptions;
            this.UseInMemoryStorage = databaseOptions?.UseInMemoryStorage ?? false;

            this.databaseService = new DatabaseService(databaseOptions.StorageDirectory, databaseName, new JsonSerializationFactory(this.Logger), databaseOptions.StorageIO, this.Logger, false, databaseOptions.StorageName);
            if (this.DatabaseOptions == null)
                return;

            if (!string.IsNullOrEmpty(databaseOptions.RunAsServerWithAddress))
            {
                RemoteAccess = new RemoteAccess();
                RemoteAccess.RunServer(databaseOptions.RunAsServerWithAddress);
            }

            this.RemoteServerAdrress = databaseOptions.UseRemoteServerAdrressForClientRequests;
            this.databaseService.DataBaseSettings.EnableCaching = this.DatabaseOptions.EnableCaching;
            this.databaseService.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates = this.DatabaseOptions.EnableFasterCachingButWithLeakyUpdates;
            this.databaseService.DataBaseSettings.MaxResponseTime = this.DatabaseOptions.MaxResponseTime;
            this.databaseService.DataBaseSettings.PrettifyDocuments = this.DatabaseOptions.PrettifyDocuments;
            this.databaseService.DataBaseSettings.DontWaitForWrites = this.DatabaseOptions.DontWaitForWrites;
            this.databaseService.DataBaseSettings.DontWaitForWritesOnCreate = this.DatabaseOptions.DontWaitForWritesOnCreate;
        }

        public string RemoteServerAdrress { get; set; }
    }
}