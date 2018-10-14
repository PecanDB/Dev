namespace PecanDB
{
    using Newtonsoft.Json;
    using PecanDb.Storage;
    using PecanDB.Remoting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;

    public class Session : ISession
    {
        internal static string DestinationFilesStorage = AppDomain.CurrentDomain.BaseDirectory + "\\FILES_STORAGE";
        private readonly DatabaseService databaseService;

        internal Session(DatabaseService databaseService, IPecanLogger logger, string remoteServerAdrress)
        {
            this.RemoteServerAdrress = remoteServerAdrress;
            this.RunAsHttpClient = !string.IsNullOrEmpty(remoteServerAdrress);
            this.Logger = logger;
            this.TrackingDictionary = new Dictionary<string, TrackedObject>();
            this.databaseService = databaseService;
            this.SessionId = Guid.NewGuid();
        }

        public bool RunAsHttpClient { get; set; }

        public string RemoteServerAdrress { get; set; }

        private Dictionary<string, TrackedObject> TrackingDictionary { set; get; }

        public FilesStorage LoadFile(string id)
        {
            this.ThrowNotPossibleRemotelyExceptionIfNecessary();
            this.Logger?.Trace(this.GetType().Name, $"Loading file {id} - {this.GetContextDescription()}");
            var obj = this.Load<FilesStorage>(id);
            return obj;
        }

        private void ThrowNotPossibleRemotelyExceptionIfNecessary()
        {
            if (this.RunAsHttpClient)
            {
                throw new Exception("This operation is not available using remote database : " + this.RemoteServerAdrress);
            }
        }

        public IEnumerable<FilesStorage> SearchFiles(Predicate<string> predicate, Func<IEnumerable<FilesStorage>, IEnumerable<FilesStorage>> query = null)
        {
            this.ThrowNotPossibleRemotelyExceptionIfNecessary();
            this.Logger?.Trace(this.GetType().Name, $"Searching with predicate {predicate} - {this.GetContextDescription()}");
            return this.Search(predicate, query);
        }

        public IEnumerable<FilesStorage> QueryFiles(Func<IEnumerable<FilesStorage>, IEnumerable<FilesStorage>> query = null)
        {
            this.ThrowNotPossibleRemotelyExceptionIfNecessary();
            this.Logger?.Trace(this.GetType().Name, $"Querying with query {query} - {this.GetContextDescription()}");
            return this.Query(query);
        }

        public Guid SessionId { get; set; }

        public IPecanLogger Logger { get; set; }

        public string SaveFile(string filePath, string destinationFolderForFiles = null)
        {
            this.ThrowNotPossibleRemotelyExceptionIfNecessary();
            this.Logger?.Trace(this.GetType().Name, $"Storing and saving file {filePath} possibly to destination {destinationFolderForFiles} - {this.GetContextDescription()}");
            StorageDatabase<PecanDocument<FilesStorage>, FilesStorage> handle = this.GetDatabaseServiceHandle<FilesStorage>(null);

            string id = handle.GetNextId().Item1;

            FilesStorage obj = this.UpdateFile(id, filePath, destinationFolderForFiles);

            return this.Save(obj, obj.Id);
        }

        public FilesStorage UpdateFile(string fileId, string updateOriginalPath, string destinationFolderForFiles = null)
        {
            this.ThrowNotPossibleRemotelyExceptionIfNecessary();
            FilesStorage obj = CreateFileObject(updateOriginalPath, destinationFolderForFiles, fileId);
            if (obj.FilePath != obj.OriginalPath)
            {
                var client = new WebClient();
                client.DownloadFile(obj.OriginalPath, obj.FilePath);
            }
            return obj;
        }

        public T Load<T>(string id, string documentName = null, bool includeDeleted = false)
        {
            if (RunAsHttpClient)
            {
                var databaseName = PecanDatabaseUtilityObj.DetermineDatabaseName<PecanDocument<T>, T>(documentName);
                var resultStr = RemoteAccess.MakeRequest<string>(RemoteServerAdrress, $"Load?id={id}&database={databaseName}");
                var resultData = string.IsNullOrEmpty(resultStr) ? default(T) : JsonConvert.DeserializeObject<T>(resultStr);
                return resultData;
            }

            this.Logger?.Trace(this.GetType().Name, $"Load document {id} of {typeof(T).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

            PecanDocument<T> result = this.LoadDocument<T>(id, documentName, includeDeleted);
            return result.DocumentEntity;
        }

        public TAs LoadAs<T, TAs>(string id, string documentName = null, bool includeDeleted = false)
        {
            if (RunAsHttpClient)
            {
                var databaseName = PecanDatabaseUtilityObj.DetermineDatabaseName<PecanDocument<T>, T>(documentName);
                var resultStr = RemoteAccess.MakeRequest<string>(RemoteServerAdrress, $"Load?id={id}&database={databaseName}");
                var resultData = string.IsNullOrEmpty(resultStr) ? default(TAs) : JsonConvert.DeserializeObject<TAs>(resultStr);
                return resultData;
            }

            this.Logger?.Trace(this.GetType().Name, $"Load document {id} of {typeof(T).Name}  as {typeof(TAs).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

            PecanDocument<TAs> result = this.LoadDocument<T, TAs>(id, documentName, includeDeleted);
            return result.DocumentEntity;
            //return default(TAs);
        }

        public TAs LoadAs<TAs>(string id, string documentName = null, bool includeDeleted = false)
        {
            //pass

            this.Logger?.Trace(this.GetType().Name, $"Load document {id} as {typeof(TAs).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

            return this.LoadAs<dynamic, TAs>(id, documentName, includeDeleted);
        }

        public dynamic Load(string id, string documentName = null, bool includeDeleted = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Load document {id} from dynamic . Supplied document name {documentName} - {this.GetContextDescription()}");

            return this.Load<dynamic>(id, documentName, includeDeleted);
        }

        public string LoadRawDocument<T>(string id, string documentName = null, bool includeDeleted = false)
        {
            if (RunAsHttpClient)
            {
                var databaseName = PecanDatabaseUtilityObj.DetermineDatabaseName<PecanDocument<T>, T>(documentName);
                var resultStr = RemoteAccess.MakeRequest<string>(RemoteServerAdrress, $"Load?id={id}&database={databaseName}&includeDeleted={includeDeleted}");
                return resultStr;
            }

            this.Logger?.Trace(this.GetType().Name, $"Load json document {id} from dynamic . Supplied document name {documentName} - {this.GetContextDescription()}");
            string result = this.LoadDocument<T, string>(id, documentName, includeDeleted, true).DocumentEntity;
            return result;
        }

        public string LoadRawDocument(string id, string documentName = null, bool includeDeleted = false)
        {
            //pass
            this.Logger?.Trace(this.GetType().Name, $"Load json document {id} from dynamic . Supplied document name {documentName} - {this.GetContextDescription()}");
            dynamic result = this.LoadDocument<dynamic>(id, documentName, includeDeleted, true).DocumentEntity.ToString();
            return result;
        }

        public string Save<T>(string documentName, T document, string id = null)
        {
            if (RunAsHttpClient)
            {
                var databaseName = PecanDatabaseUtilityObj.DetermineDatabaseName<PecanDocument<T>, T>(documentName);
                var data = JsonConvert.SerializeObject(document);
                var resultStr = RemoteAccess.MakeRequest<string>(RemoteServerAdrress, $"Save?data={data}&database={databaseName}");
                return resultStr;
            }

            this.Logger?.Trace(this.GetType().Name, $"STORING NEW Document with id {id} of {documentName} type {typeof(T).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

            if (IsAnonymousObject<T>())
            {
                this.Logger?.Trace(this.GetType().Name, $"STORING NEW Document determined to be anonymous with id {id} of {documentName} type {typeof(T).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

                StorageDatabase<PecanDocument<object>, object> handle = this.GetDatabaseServiceHandle<object>(documentName);
                var data = new PecanDocument<object>
                {
                    DocumentEntity = document.ToDynamic()
                };

                PecanDocument<object> result = handle.Create(data, false, id);

                this.Logger?.Trace(this.GetType().Name, $"STORING NEW successfully obtained final Order id {result?.Id} after the storing Document with id {id} of {documentName} type {typeof(T).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

                return result.Id;
            }
            else
            {
                this.Logger?.Trace(this.GetType().Name, $"STORING NEW storing Document determined to be NOT anonymous with id {id} of {documentName} type {typeof(T).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

                StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);
                var data = new PecanDocument<T>
                {
                    DocumentEntity = document
                };

                PecanDocument<T> result = handle.Create(data, false, id);
                this.Logger?.Trace(this.GetType().Name, $"STORING NEW successfully obtained final Order id {result?.Id} after the storing Document with id {id} of {documentName} type {typeof(T).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

                return result.Id;
            }
        }

        public string Save<T>(T document, string id)
        {
            return this.Save(null, document, id);
        }

        public string Save<T>(T document)
        {
            return this.Save(null, document, null);
        }

        public bool SaveChanges(bool saveWithEtag = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Saving changes. Use etag : {saveWithEtag} - {this.GetContextDescription()}");

            return this.databaseService.Transaction(
                db =>
                {
                    //todo move this to server side
                    if (saveWithEtag)
                        if (!this.CheckIfEtagHasChanged(db))
                        {
                            this.Logger?.Trace(this.GetType().Name, $"Etag has already changed before completeing saving changes. Use etag : {saveWithEtag} - {this.GetContextDescription()}");

                            return false;
                        }
                    this.SaveModelChangesAndTryUpdateEtag(saveWithEtag, db);

                    this.Logger?.Trace(this.GetType().Name, $"Successfully saved changes Using etag : {saveWithEtag}  - {this.GetContextDescription()}");

                    return true;
                });
        }

        public IEnumerable<PecanDocument<T>> QueryDocument<T>(Func<IEnumerable<PecanDocument<T>>, IEnumerable<PecanDocument<T>>> query, string documentName = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Query document {documentName}  Supplied document name {documentName} - {this.GetContextDescription()}");

            StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);
            return handle.LoadAll(query);
        }

        public IEnumerable<T> Query<T>(Func<IEnumerable<T>, IEnumerable<T>> query, string documentName = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Query document {documentName}  Supplied document name {documentName} - {this.GetContextDescription()}");

            StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);
            return handle.LoadAll(d => { return query(d.Select(x => x.DocumentEntity)); });
        }

        public IEnumerable<T> Search<T>(Predicate<string> predicate, Func<IEnumerable<T>, IEnumerable<T>> query = null, string documentName = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Search document {documentName}  Supplied document name {documentName} - {this.GetContextDescription()}");

            StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);
            IEnumerable<PecanDocument<T>> result = handle.Search(
                predicate,
                d =>
                {
                    return query?.Invoke(d.Select(x => x.DocumentEntity)).Select(
                        x => new PecanDocument<T>
                        {
                            DocumentEntity = x
                        }) ?? d;
                });
            return result.Select(x => x.DocumentEntity);
        }

        public void Dispose()
        {
            this.TrackingDictionary = new Dictionary<string, TrackedObject>();
            this.databaseService.Dispose();
        }

        public void Delete<T>(string id, string documentName = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Delete document {documentName} with id {id}  Supplied document name {documentName} - {this.GetContextDescription()}");

            StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);
            handle.Delete(id);
        }

        public void DeleteForever<T>(string id, string documentName = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Delete document forever {documentName} with id {id}  Supplied document name {documentName} - {this.GetContextDescription()}");

            StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);
            handle.DeleteForever(id);
        }

        private string GetContextDescription()
        {
            return $"Session Id {this.SessionId} TrackingDictionary count : {this.TrackingDictionary?.Count},MaxResponseTime: {this.databaseService?.DataBaseSettings?.MaxResponseTime}, PrettifyDocuments: {this.databaseService?.DataBaseSettings?.PrettifyDocuments}, EnableCaching: {this.databaseService?.DataBaseSettings?.EnableCaching}, storage mechanism : {this.databaseService?.DataBaseSettings?.StorageMechanismMech}, EnableFasterCachingButWithLeakyUpdates:  {this.databaseService?.DataBaseSettings?.EnableFasterCachingButWithLeakyUpdates}, Common storage mechanism {this.databaseService?.DataBaseSettings?.AkkaCommonStorageMechanism} DontWaitForWrites:";
        }

        private static FilesStorage CreateFileObject(string filePath, string destinationFolderForFiles, string id)
        {
            destinationFolderForFiles = destinationFolderForFiles ?? DestinationFilesStorage;
            string fileName = Path.GetFileName(filePath);
            string fullpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + destinationFolderForFiles;
            string fileExtention = Path.GetExtension(filePath).ToLower().TrimStart('.');
            string destinationPath = fullpath + "\\" + fileName;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            if (!Directory.Exists(fullpath))
                Directory.CreateDirectory(fullpath);

            var obj = new FilesStorage
            {
                FilePath = destinationPath,
                FileType = fileExtention,
                Name = fileNameWithoutExtension,
                OriginalPath = filePath,
                Id = id
            };
            return obj;
        }

        [Obsolete("Very very slow, its reflection")]
        private PecanDocument<T> LoadDocumentWithReflection<T>(string id, string documentName = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Load document {id} of {typeof(T).Name}  using reflection. Supplied document name {documentName} - {this.GetContextDescription()}");

            CallStorageMethod<bool>(typeof(PecanDocument<T>), typeof(T), this.databaseService.DirectDocumentManipulator, documentName, "InitializeDatabaseCacheIfNotReady", null);
            var result = CallStorageMethod<PecanDocument<T>>(typeof(PecanDocument<T>), typeof(T), this.databaseService.DirectDocumentManipulator, documentName, "Load", new object[] { id, false });
            return this.TrackDocument(id, documentName, result, false);
        }

        private StorageDatabase<PecanDocument<T>, T> GetDatabaseServiceHandle<T>(string documentName)
        {
            this.Logger?.Trace(this.GetType().Name, $"Get database handle. Supplied document name {documentName} - {this.GetContextDescription()}");

            StorageDatabase<PecanDocument<T>, T> handle = null;
            handle = this.databaseService.Documents<PecanDocument<T>, T>(documentName);
            handle.InitializeDatabaseCacheIfNotReady();

            return handle;
        }

        private PecanDocument<TAs> LoadDocument<T, TAs>(string id, string documentName = null, bool includeDeleted = false, bool expectFullJsonStringReturned = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Load document {id} of {typeof(T).Name}  as {typeof(TAs).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

            PecanDocument<TAs> finalResult = null;
            if (IsAnonymousObject<T>())
            {
                StorageDatabase<PecanDocument<object>, object> handle = this.GetDatabaseServiceHandle<object>(documentName);
                if (expectFullJsonStringReturned)
                {
                    string result = handle.LoadJson(id, includeDeleted, expectFullJsonStringReturned);
                    finalResult = new PecanDocument<TAs>
                    {
                        DocumentEntity = (TAs)(object)result
                    };
                }
                else
                {
                    var result = handle.Load<PecanDocument<TAs>>(id, includeDeleted);

                    finalResult = result;
                }

                //todo as is not tracked  finalResult = this.TrackDocument(id, documentName, result, false);
            }
            else
            {
                this.Logger?.Trace(this.GetType().Name, $"Loading document {id} of {typeof(T).Name}  as {typeof(TAs).Name} . Supplied document name {documentName} - {this.GetContextDescription()}");
                StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);

                string result = handle.LoadJson(id, includeDeleted, expectFullJsonStringReturned);

                if (expectFullJsonStringReturned)
                    finalResult = new PecanDocument<TAs>
                    {
                        DocumentEntity = (TAs)(object)result
                    };
                else
                    finalResult = this.databaseService.DataBaseSettings.StorageMechanismMech.FileSystem.SerializationFactory.DeserializeObject<PecanDocument<TAs>>(result);
            }
            return finalResult;
        }

        private PecanDocument<T> LoadDocument<T>(string id, string documentName = null, bool includeDeleted = false, bool expectFullJsonStringReturned = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Load document {id} of {typeof(T).Name}. Supplied document name {documentName} - {this.GetContextDescription()}");

            PecanDocument<T> finalResult = null;
            if (IsAnonymousObject<T>())
            {
                StorageDatabase<PecanDocument<object>, object> handle = this.GetDatabaseServiceHandle<object>(documentName);
                if (expectFullJsonStringReturned)
                {
                    string result = handle.LoadJson(id, includeDeleted, expectFullJsonStringReturned);
                    finalResult = new PecanDocument<T>
                    {
                        DocumentEntity = (T)(object)result
                    };
                }
                else
                {
                    PecanDocument<object> result = handle.Load(id, includeDeleted, expectFullJsonStringReturned);
                    finalResult = this.TrackDocument(id, documentName, result, false) as PecanDocument<T>;
                }
            }
            else
            {
                StorageDatabase<PecanDocument<T>, T> handle = this.GetDatabaseServiceHandle<T>(documentName);
                PecanDocument<T> result = handle.Load(id, includeDeleted);
                finalResult = this.TrackDocument(id, documentName, result, false);
            }
            return finalResult;
        }

        private static bool IsAnonymousObject<T>()
        {
            bool result = typeof(T).FullName.Contains("System.Object") || typeof(T).FullName.Contains("AnonymousType");

            return result;
        }

        private PecanDocument<T> TrackDocument<T>(string id, string documentName, PecanDocument<T> result, bool isNew)
        {
            this.Logger?.Trace(this.GetType().Name, $"Tracking document {id} of {typeof(T).Name} . Is new :{isNew}. Supplied document name {documentName} - {this.GetContextDescription()}");

            if (result == null)
            {
                this.Logger?.Trace(this.GetType().Name, $"Error : Document with id {id}:{documentName} doesnt exist while trying to track it. Is new :{isNew}. Supplied document name {documentName} - {this.GetContextDescription()}");

                throw new Exception($"Document with id {id}:{documentName} doesnt exist");
            }
            if (this.TrackingDictionary.ContainsKey(id))
            {
                this.Logger?.Trace(this.GetType().Name, $"Document {id}  {typeof(T).Name} . Is new :{isNew} is already in the tracking dictionary. Supplied document name {documentName} - {this.GetContextDescription()}");
            }
            else
            {
                this.Logger?.Trace(this.GetType().Name, $"Adding document {id}  {typeof(T).Name}  new :{isNew} to tracking dictionary. Supplied document name {documentName} - {this.GetContextDescription()}");

                this.TrackingDictionary.Add(id, new TrackedObject(result, typeof(T), result.ETag, result.DocumentName, isNew));
            }

            return this.TrackingDictionary[id].Document as PecanDocument<T>;
        }

        private void SaveModelChangesAndTryUpdateEtag(bool saveWithEtag, DirectDocumentManipulator db)
        {
            this.Logger?.Trace(this.GetType().Name, $"save model. use direct db manipulator. Try update with etag {saveWithEtag} - {this.GetContextDescription()}");

            foreach (KeyValuePair<string, TrackedObject> obj in this.TrackingDictionary)
            {
                object d = obj.Value.Document;
                Type docType = obj.Value.DocumentType;
                string etag = obj.Value.ETag;
                Type subtype = d.GetType();

                string existingEtag;
                if (saveWithEtag)
                {
                    this.Logger?.Trace(this.GetType().Name, $"Saving  {d} type {docType?.Name} {subtype?.Name} with etag {etag} use direct db manipulator. Try update with etag {saveWithEtag} - {this.GetContextDescription()}");

                    existingEtag = CallStorageMethod<string>(subtype, docType, db, obj.Value.DocumentName, "Update", new[] { d, etag });
                }
                else
                {
                    this.Logger?.Trace(this.GetType().Name, $"Saving  {d} type {docType?.Name} {subtype?.Name} with NO etag {etag} use direct db manipulator. Try update with etag {saveWithEtag} - {this.GetContextDescription()}");

                    existingEtag = CallStorageMethod<string>(subtype, docType, db, obj.Value.DocumentName, "Update", new[] { d, null });
                }
                obj.Value.ETag = existingEtag;

                this.Logger?.Trace(this.GetType().Name, $"Successfully Saved  {d} type {docType?.Name} with new etag {existingEtag} use direct db manipulator. Try update with etag {saveWithEtag} - {this.GetContextDescription()}");
            }
        }

        private bool CheckIfEtagHasChanged(DirectDocumentManipulator db)
        {
            this.Logger?.Trace(this.GetType().Name, $"Check if etag has changed - {this.GetContextDescription()}");

            foreach (KeyValuePair<string, TrackedObject> obj in this.TrackingDictionary)
            {
                Type docType = obj.Value.DocumentType;
                string etag = obj.Value.ETag;
                string id = obj.Key;
                Type subtype = obj.Value.Document.GetType();

                string invokeMethod = "GetEtag";
                object[] argument = { id, false };

                string existingEtag = CallStorageMethod<string>(subtype, docType, db, obj.Value.DocumentName, invokeMethod, argument);

                if (existingEtag != etag)
                {
                    this.Logger?.Trace(this.GetType().Name, $"Etag has changed for {id} {docType?.Name} {subtype?.Name} from {etag} to {existingEtag} - {this.GetContextDescription()}");
                    return false;
                }
                else
                {
                    this.Logger?.Trace(this.GetType().Name, $"Etag {etag}  has not changed for {id} {docType?.Name} {subtype?.Name} - {this.GetContextDescription()}");
                }
            }
            return true;
        }

        private static T CallStorageMethod<T>(Type documentWithObjectType, Type objectOnlyType, DirectDocumentManipulator db, string documentName, string invokeMethod, object[] argument)
        {
            MethodInfo method = typeof(DirectDocumentManipulator).GetMethod("GetDBRef")
                .MakeGenericMethod(documentWithObjectType, objectOnlyType);
            object dbref = method.Invoke(db, new object[] { documentName });
            MethodInfo method2 = dbref.GetType().GetMethod(invokeMethod);
            var result = (T)method2.Invoke(dbref, argument);
            return result;
        }

        public string GetETagFor<T>(string id, string documentName = null)
        {
            if (RunAsHttpClient)
            {
                var databaseName = PecanDatabaseUtilityObj.DetermineDatabaseName<PecanDocument<T>, T>(documentName);
                var resultStr = RemoteAccess.MakeRequest<string>(RemoteServerAdrress, $"GetETagFor?id={id}&database={databaseName}");
                return resultStr;
            }
            this.Logger?.Trace(this.GetType().Name, $"Get etag for document {documentName} with id {id}  Supplied document name {documentName} - {this.GetContextDescription()}");

            PecanDocument<T> result = this.LoadDocument<T>(id, documentName);

            this.Logger?.Trace(this.GetType().Name, $"Get etag {result?.ETag} from document {documentName} with id {id}  Supplied document name {documentName} - {this.GetContextDescription()}");

            return result.ETag;
        }
    }
}