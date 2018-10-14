namespace PecanDb.Storage.StorageSystems
{
    using PecanDb.Storage.Contracts;
    using PecanDB;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public class FileStorageSystem : IStorageSystem
    {
        private readonly IStorageIO _fileIO;
        private readonly ConcurrentDictionary<Tuple<Type, string>, string> cache = new ConcurrentDictionary<Tuple<Type, string>, string>();
        private readonly ConcurrentDictionary<Tuple<Type, string>, object> cacheWithInMemoryReferences = new ConcurrentDictionary<Tuple<Type, string>, object>();
        private readonly DataBaseSettings DataBaseSettings;

        public FileStorageSystem(DataBaseSettings DdtaBaseSettings, IStorageIO fileIO, ISerializationFactory sf, IPecanLogger logger)
        {
            this.Logger = logger;
            this.DataBaseSettings = DdtaBaseSettings;
            this._fileIO = fileIO;
            this.SerializationFactory = sf;
        }

        public ISerializationFactory SerializationFactory { set; get; }

        public string ReadJsonFromFile<TDocumentWithObject, TFromFileTDocumentWithObject>(string fileName, bool ignoreCaching = false)
        {
            string json = "";
            this.Logger?.Trace(this.GetType().Name, $"Read object from file {fileName}  Ignore caching :{ignoreCaching} {this.GetContextDescription()} ");

            if (!ignoreCaching && this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates)
                json = this.SerializationFactory.SerializeObject(this.cacheWithInMemoryReferences[new Tuple<Type, string>(typeof(TFromFileTDocumentWithObject), fileName)], true).ToString();
            else if (!ignoreCaching && this.DataBaseSettings.EnableCaching)
                json = this.cache[new Tuple<Type, string>(typeof(TFromFileTDocumentWithObject), fileName)];
            else
                json = this.ReadFile<TFromFileTDocumentWithObject>(fileName, ignoreCaching);

            return json;
        }

        public TDocumentWithObject ReadObjectFromFile<TDocumentWithObject, TFromFileTDocumentWithObject>(string fileName, bool ignoreCaching = false)
        {
            TDocumentWithObject data = default(TDocumentWithObject);
            this.Logger?.Trace(this.GetType().Name, $"Read object from file {fileName}  Ignore caching :{ignoreCaching} {this.GetContextDescription()} ");

            if (!ignoreCaching && this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates)
            {
                data = (TDocumentWithObject)this.cacheWithInMemoryReferences[new Tuple<Type, string>(typeof(TFromFileTDocumentWithObject), fileName)];
            }
            else if (!ignoreCaching && this.DataBaseSettings.EnableCaching)
            {
                data = this.SerializationFactory.DeserializeObject<TDocumentWithObject>(this.cache[new Tuple<Type, string>(typeof(TFromFileTDocumentWithObject), fileName)]);
            }
            else
            {
                string json = this.ReadFile<TFromFileTDocumentWithObject>(fileName, ignoreCaching);
                if (string.IsNullOrWhiteSpace(json))
                {
                    this.FileDelete<TFromFileTDocumentWithObject>(fileName);
                    //log fatal
                    data = default(TDocumentWithObject);
                }
                data = this.SerializationFactory.DeserializeObject<TDocumentWithObject>(json);
            }

            return data;
        }

        public void WriteToFile<TDocumentWithObject>(string fileNme, TDocumentWithObject content)
        {
            this.Logger?.Trace(this.GetType().Name, $"Write to file {fileNme}, {this.GetContextDescription()} ");

            //todo prettify
            string json = (this.DataBaseSettings.PrettifyDocuments ? this.SerializationFactory.SerializeObject(content, true) : this.SerializationFactory.SerializeObject(content)).ToString();

            this.WriteToFile(fileNme, json);
            this.UpdateCache<TDocumentWithObject>(fileNme, json);
        }

        public IPecanLogger Logger { get; set; }

        public void InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(string getDbFolder)
        {
            this.Logger?.Trace(this.GetType().Name, $"Initializing cache if not ready with {getDbFolder} {this.GetContextDescription()} ");

            if (this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates)
                if (this.cacheWithInMemoryReferences.IsEmpty)
                    this.LoadFilesIntoMemory<TDocumentWithObject>(getDbFolder);
            if (this.DataBaseSettings.EnableCaching)
                if (this.cache.IsEmpty)
                    this.LoadFilesIntoMemory<TDocumentWithObject>(getDbFolder);
        }

        public IEnumerable<string> EnumerateFilesNotFromCache<TDocumentWithObject>(string getDbFolder)
        {
            this.Logger?.Trace(this.GetType().Name, $"Enumerate files not from cache {this.GetContextDescription()} ");

            return this._fileIO.DirectoryEnumerateFiles(getDbFolder);
        }

        public IEnumerable<string> EnumerateFiles<TDocumentWithObject>(string getDbFolder)
        {
            this.Logger?.Trace(this.GetType().Name, $"Enumerate files. Folder {getDbFolder}  {this.GetContextDescription()} ");

            if (this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates)
            {
                this.Logger?.Trace(this.GetType().Name, $"Enumerate files when EnableFasterCachingButWithLeakyUpdates . Folder {getDbFolder}  {this.GetContextDescription()} ");

                return this.cacheWithInMemoryReferences.Where(x => x.Key.Item1 == typeof(TDocumentWithObject)).Select(x => x.Key.Item2);
            }
            if (this.DataBaseSettings.EnableCaching)
            {
                this.Logger?.Trace(this.GetType().Name, $"Enumerate files when EnableCaching . Folder {getDbFolder}  {this.GetContextDescription()} ");

                return this.cache.Where(x => x.Key.Item1 == typeof(TDocumentWithObject)).Select(x => x.Key.Item2);
            }
            return this.EnumerateFilesNotFromCache<TDocumentWithObject>(getDbFolder);
        }

        public IEnumerable<TDocumentWithObject> GetAll<TDocumentWithObject, TObjectOnly>(string folder, Func<TDocumentWithObject, bool> where, Predicate<string> search = null)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>

        {
            this.Logger?.Trace(this.GetType().Name, $"Get all . Folder {folder} {this.GetContextDescription()} ");

            IEnumerable<string> query = this.EnumerateFiles<TDocumentWithObject>(folder);

            IEnumerable<TDocumentWithObject> data = query
                .Select(x => this.ReadObjectFromFile<TDocumentWithObject, TDocumentWithObject>(x));
            if (where != null)
                data = data.Where(where);

            if (search != null)
                data = WhereAtLeastOneProperty<TDocumentWithObject, TObjectOnly, string>(data, search);

            return data;
        }

        public bool DirectoryExists(string db)
        {
            this.Logger?.Trace(this.GetType().Name, $"Check if directory exists {db} {this.GetContextDescription()} ");

            return this._fileIO.DirectoryExists(db);
        }

        public void CreateDirectory(string db)
        {
            this.Logger?.Trace(this.GetType().Name, $"Creating directory {db} {this.GetContextDescription()} ");

            this._fileIO.CreateDirectory(db);
        }

        public bool FileExists<TDocumentWithObject>(string toFileName, bool ignoreCache)
        {
            this.Logger?.Trace(this.GetType().Name, $"Check if file exists {toFileName} , ignore cache {ignoreCache} . {this.GetContextDescription()} ");

            if (!ignoreCache)
            {
                if (this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates)
                {
                    this.Logger?.Trace(this.GetType().Name, $"Check if file exists when EnableFasterCachingButWithLeakyUpdates {toFileName} , ignore cache {ignoreCache} . {this.GetContextDescription()} ");

                    var key = new Tuple<Type, string>(typeof(TDocumentWithObject), toFileName);
                    return this.cacheWithInMemoryReferences.ContainsKey(key);
                }
                if (this.DataBaseSettings.EnableCaching)
                {
                    this.Logger?.Trace(this.GetType().Name, $"Check if file exists when EnableCaching {toFileName} , ignore cache {ignoreCache} . {this.GetContextDescription()} ");

                    var key = new Tuple<Type, string>(typeof(TDocumentWithObject), toFileName);
                    return this.cache.ContainsKey(key);
                }
            }

            return this._fileIO.FileExists(toFileName);
        }

        public void FileDelete<TDocumentWithObject>(string toFileName)
        {
            this.Logger?.Trace(this.GetType().Name, $"Delete file {toFileName} {this.GetContextDescription()} ");

            if (this.FileExists<TDocumentWithObject>(toFileName, false))
                this._fileIO.FileDelete(toFileName);
            var key = new Tuple<Type, string>(typeof(TDocumentWithObject), toFileName);
            if (this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates)
            {
                this.Logger?.Trace(this.GetType().Name, $"Delete file when EnableFasterCachingButWithLeakyUpdates {toFileName} {this.GetContextDescription()} ");

                object val;
                if (this.cacheWithInMemoryReferences.ContainsKey(key))
                    this.cacheWithInMemoryReferences.TryRemove(key, out val);
            }
            if (this.DataBaseSettings.EnableCaching)
            {
                this.Logger?.Trace(this.GetType().Name, $"Delete file when EnableCaching {toFileName} {this.GetContextDescription()} ");

                string val;
                if (this.cache.ContainsKey(key))
                    this.cache.TryRemove(key, out val);
            }
        }

        private string GetContextDescription()
        {
            return $"MaxResponseTime: {this.DataBaseSettings?.MaxResponseTime} , PrettifyDocuments: {this.DataBaseSettings?.PrettifyDocuments} , Serialization Factory : {this.SerializationFactory.GetType().Name}, File IO : {this._fileIO.GetType().Name}, EnableCaching {this.DataBaseSettings.EnableCaching}, EnableFasterCachingButWithLeakyUpdates {this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates} DontWaitForWrites {this.DataBaseSettings.DontWaitForWrites}";
        }

        private string ReadFile<TDocumentWithObject>(string fileName, bool ignoreCache)
        {
            this.Logger?.Trace(this.GetType().Name, $"Read file {fileName}  Ignore caching :{ignoreCache} {this.GetContextDescription()} ");

            if (!this.FileExists<TDocumentWithObject>(fileName, ignoreCache))
                return "{}";
            else
                return this._fileIO.FileReadAllText(fileName);
        }

        private void WriteToFile(string fileName, string content)
        {
            this.Logger?.Debug(this.GetType().Name, $"Write to file {fileName} {this.GetContextDescription()} ", null, content);

            this._fileIO.FileWriteAllText(fileName, content);
        }

        private void UpdateCache<TDocumentWithObject>(string fileNme, string json)
        {
            this.Logger?.Trace(this.GetType().Name, $"Update file {fileNme}  {this.GetContextDescription()} ", null, json);

            var key = new Tuple<Type, string>(typeof(TDocumentWithObject), fileNme);
            if (this.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates)
            {
                this.Logger?.Trace(this.GetType().Name, $"Update file when EnableFasterCachingButWithLeakyUpdates {fileNme}  {this.GetContextDescription()} ", null, json);

                var val = this.SerializationFactory.DeserializeObject<TDocumentWithObject>(json);

                if (this.cacheWithInMemoryReferences.ContainsKey(key))
                    this.cacheWithInMemoryReferences[key] = val;
                else
                    this.cacheWithInMemoryReferences.TryAdd(key, val);
            }

            if (this.DataBaseSettings.EnableCaching)
                if (this.cache.ContainsKey(key))
                    this.cache[key] = json;
                else
                    this.cache.TryAdd(key, json);
        }

        private IEnumerable<string> LoadFilesIntoMemory<TDocumentWithObject>(string getDbFolder)
        {
            this.Logger?.Trace(this.GetType().Name, $"Load files into memory from folder {getDbFolder} {this.GetContextDescription()} ");

            IEnumerable<string> result = this._fileIO.DirectoryEnumerateFiles(getDbFolder);
            Task.Run(
                () =>
                {
                    result.ToList().ForEach(
                        x =>
                        {
                            this.Logger?.Trace(this.GetType().Name, $"Load file {x} into memory from folder {getDbFolder} {this.GetContextDescription()} ");

                            string json = this.ReadFile<TDocumentWithObject>(x, true);
                            this.UpdateCache<TDocumentWithObject>(x, json);
                            this.Logger?.Trace(this.GetType().Name, $"Cache updated :  {x} into memory from folder {getDbFolder} {this.GetContextDescription()} ");
                        });
                }).Wait();
            return result;
        }

        internal static IEnumerable<TDocumentWithObject> WhereAtLeastOneProperty<TDocumentWithObject, TObjectOnly, PropertyType>(IEnumerable<TDocumentWithObject> source, Predicate<PropertyType> predicate)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            PropertyInfo[] metadataProperties = typeof(TDocumentWithObject).GetProperties().Where(prop => prop.CanRead && prop.PropertyType == typeof(PropertyType)).ToArray();
            PropertyInfo[] documentProperties = typeof(TObjectOnly).GetProperties().Where(prop => prop.CanRead && prop.PropertyType == typeof(PropertyType)).ToArray();

            return source.Where(
                item =>
                    metadataProperties.Any(prop => PropertySatisfiesPredicate(predicate, item, prop)) ||
                    documentProperties.Any(prop => PropertySatisfiesPredicate(predicate, item.DocumentEntity, prop))
            );
        }

        private static bool PropertySatisfiesPredicate<TDocumentWithObject, PropertyType>(Predicate<PropertyType> predicate, TDocumentWithObject item, PropertyInfo prop)

        {
            try
            {
                return predicate((PropertyType)prop.GetValue(item));
            }
            catch
            {
                return false;
            }
        }
    }
}