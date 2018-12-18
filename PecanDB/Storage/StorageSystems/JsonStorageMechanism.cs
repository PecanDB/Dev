namespace PecanDb.Storage.StorageSystems
{
    using PecanDb.Storage.Contracts;
    using PecanDB;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class JsonStorageMechanism : IStorageMechanism
    {
        public JsonStorageMechanism(IStorageSystem fileSystem, string databaseName, string rootFolder, IPecanLogger logger)
        {
            this.Logger = logger;

            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));
            this._rootFolder = rootFolder;
            this.db = this._rootFolder + "\\" + databaseName;

            this.FileSystem = fileSystem;
            if (!this.FileSystem.DirectoryExists(this.db))
                this.FileSystem.CreateDirectory(this.db);

            this.Logger?.Trace(this.GetType().Name, $"Initialized {this.GetType().Name} {this.GetContextDescription()}");
        }

        private string db { get; }

        private string _rootFolder { get; }

        public IStorageSystem FileSystem { set; get; }

        public void Set<TDocumentWithObject, TObjectOnly>(string context, TDocumentWithObject dbject, string id, string etag, string documentDirectory, bool noDuplicates)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to set with etag {etag} with document name {documentDirectory} and id {id} no duplicates:{noDuplicates} {this.GetType().Name} {this.GetContextDescription()}");

            if (noDuplicates && this.Exists<TDocumentWithObject>(id, documentDirectory))
            {
                this.Logger?.Error(this.GetType().Name, $"Error : Document directory {documentDirectory} with id {id} already exisst {this.GetType().Name} {this.GetContextDescription()}");

                throw new Exception($"Document with id {id}:{documentDirectory} already exists");
            }

            this.CheckForEtag<TDocumentWithObject, TObjectOnly>(id, etag, documentDirectory);
            dbject.ETag = Guid.NewGuid().ToString();
            this.FileSystem.WriteToFile(this.ToFileName<TDocumentWithObject>(id, documentDirectory), dbject);
            this.Logger?.Trace(this.GetType().Name, $"Set Succeeded with etag {etag} with document name {documentDirectory} and id {id} no duplicates:{noDuplicates} {this.GetType().Name} {this.GetContextDescription()}");
        }

        public TDocumentWithObject Get<TDocumentWithObject, TObjectOnly, TWithFileTDocumentWithObject>(string id, string documentDirectory, bool expectFullJsonStringReturned = false)

        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to get with document name {documentDirectory} and id {id} {this.GetType().Name} {this.GetContextDescription()}");

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            string name = this.ToFileName<TWithFileTDocumentWithObject>(id, documentDirectory);

            if (!this.Exists<TWithFileTDocumentWithObject>(id, documentDirectory))
            {
                this.Logger?.Error(this.GetType().Name, $"Document not found {name} when trying to get with document name {documentDirectory} and id {id} {this.GetType().Name} {this.GetContextDescription()}");

                return default(TDocumentWithObject);
            }

            if (expectFullJsonStringReturned)
            {
                this.Logger?.Trace(this.GetType().Name, $"raw document is being loaded from {name} document name {documentDirectory} and id {id} {this.GetType().Name} {this.GetContextDescription()}");

                string jsonData = this.FileSystem.ReadJsonFromFile<TDocumentWithObject, TWithFileTDocumentWithObject>(name);

                //todo doing this coz its faster than passing it through deserializer
                return (TDocumentWithObject)(object)jsonData;
            }
            TDocumentWithObject data = this.FileSystem.ReadObjectFromFile<TDocumentWithObject, TWithFileTDocumentWithObject>(name);
            return data;
        }

        public IEnumerable<TDocumentWithObject> GetAll<TDocumentWithObject, TObjectOnly>(Func<TDocumentWithObject, bool> where, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to get all with document name {documentDirectory}  {this.GetType().Name} {this.GetContextDescription()}");

            if (where == null)
                throw new ArgumentNullException(nameof(where));

            return this.FileSystem.GetAll<TDocumentWithObject, TObjectOnly>(this.GetDBFolder(documentDirectory), where);
        }

        public IPecanLogger Logger { get; set; }

        public void InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(string documentDirectory)

        {
            this.Logger?.Trace(this.GetType().Name, $"Initialize {typeof(TDocumentWithObject).Name} if not ready {this.GetType().Name} {this.GetContextDescription()}");

            this.FileSystem.InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(this.GetDBFolder(documentDirectory));
        }

        public IEnumerable<TDocumentWithObject> Search<TDocumentWithObject, TObjectOnly>(Predicate<string> search, Func<TDocumentWithObject, bool> where, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to search with rpedicate {search} with document name {documentDirectory}  {this.GetType().Name} {this.GetContextDescription()}");

            return this.FileSystem.GetAll<TDocumentWithObject, TObjectOnly>(this.GetDBFolder(documentDirectory), where, search);
        }

        //todo bug it also loads deleted files
        public IEnumerable<string> GetAll<TDocumentWithObject>(string documentDirectory)
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to get all with document name {documentDirectory} {this.GetType().Name} {this.GetContextDescription()}");

            IEnumerable<string> filePaths = this.FileSystem.EnumerateFiles<TDocumentWithObject>(this.GetDBFolder(documentDirectory)).Select(x => Path.GetFileNameWithoutExtension(x));

            return filePaths;
        }

        public long Count<TDocumentWithObject>(string documentDirectory, bool notFromCache)
        {
            long result = notFromCache ? this.FileSystem.EnumerateFilesNotFromCache<TDocumentWithObject>(this.GetDBFolder(documentDirectory)).Count() : this.FileSystem.EnumerateFiles<TDocumentWithObject>(this.GetDBFolder(documentDirectory)).Count();

            this.Logger?.Trace(this.GetType().Name, $"Got current count as {result} for {typeof(TDocumentWithObject).Name} with document name {documentDirectory} not frome cache :{notFromCache} {this.GetType().Name} {this.GetContextDescription()}");

            return result;
        }

        public bool Exists<TDocumentWithObject>(string id, string documentDirectory)
        {
            this.Logger?.Trace(this.GetType().Name, $"Checking if exists document name {documentDirectory} with id {id} {this.GetType().Name} {this.GetContextDescription()}");

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            bool result = this.FileSystem.FileExists<TDocumentWithObject>(this.ToFileName<TDocumentWithObject>(id, documentDirectory), false);

            this.Logger?.Trace(this.GetType().Name, $"Checked and found {result} that there exist document name {documentDirectory} with id {id} {this.GetType().Name} {this.GetContextDescription()}");

            return result;
        }

        public void Delete<TDocumentWithObject, TObjectOnly>(string id, string etag, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Deleting document name {documentDirectory} with id {id} with etag {etag} {this.GetType().Name} {this.GetContextDescription()}");

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            this.CheckForEtag<TDocumentWithObject, TObjectOnly>(id, etag, documentDirectory);
            this.FileSystem.FileDelete<TDocumentWithObject>(this.ToFileName<TDocumentWithObject>(id, documentDirectory));
        }

        private string GetContextDescription()
        {
            return $" {this.db} {this.FileSystem?.GetType().Name}{this._rootFolder}";
        }

        private string GetSafeFilename(string filename)
        {
            return filename.Replace("`1", "").Replace("`2", "");
        }

        private string GetDBFolder(string documentName)
        {
            string folder = this.db + "\\" + this.GetSafeFilename(documentName);
            folder = folder.Replace("/", "\\").Replace("\\\\", "\\");
            if (!this.FileSystem.DirectoryExists(folder))
                this.FileSystem.CreateDirectory(folder);

            this.Logger?.Trace(this.GetType().Name, $"Got DB Folder as {folder} with document name {documentName} {this.GetType().Name} {this.GetContextDescription()}");
            
           //todo need to fix.this fixes used of external storage
            //without this, enabling caching breaks
            //with this, local file system breaks
            // folder=Path.GetFileNameWithoutExtension(folder);

            return folder;
        }

        private string ToFileName<TDocumentWithObject>(string id, string documentName)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            string file = this.GetDBFolder(documentName) + "\\" + id.ToLower().Trim() + ".pecan";

            this.Logger?.Trace(this.GetType().Name, $"Got File name as {file} with document name {documentName} and id {id} {this.GetType().Name} {this.GetContextDescription()}");

            return file;
        }

        private void CheckForEtag<TDocumentWithObject, TObjectOnly>(string id, string etag, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Checking etag for document name {documentDirectory} with id {id} with etag {etag} {this.GetType().Name} {this.GetContextDescription()}");

            if (etag != null)
            {
                TDocumentWithObject doc = this.Get<TDocumentWithObject, TObjectOnly, TDocumentWithObject>(id, documentDirectory);
                if (doc.ETag != etag)
                {
                    this.Logger?.Trace(this.GetType().Name, $"Found etag changed for document name {documentDirectory} with id {id} with etag {etag} {this.GetType().Name} {this.GetContextDescription()}");

                    throw new Exception("Document has been modified since last load. Please reload document again before making update");
                }
            }
        }
    }
}