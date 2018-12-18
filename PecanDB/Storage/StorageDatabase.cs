namespace PecanDb.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using PecanDB;

    public class StorageDatabase<TDocumentWithObject, TObjectOnly> : IDisposable
        where TDocumentWithObject : IStandardDocument<TObjectOnly>
    {
        static readonly object CurrentCountLock = new object();
        static long CurrentCount;
        readonly IPecanLogger Logger;
        readonly IStorageMechanism storageMechanism;

        string DocumentName { get; }

        public bool InitializeDatabaseCacheIfNotReady()
        {
            this.Logger?.Trace(this.GetType().Name, $"Initializing database if not ready. Document Name {this.DocumentName}");
            this.storageMechanism.InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(this.DocumentName);
            return true;
        }

        public TDocumentWithObject Create(TDocumentWithObject dbObject, bool waitForCreation = false, string id = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"STORING NEW Supplied Id {id} and to wait for creation {waitForCreation}. Document Name {this.DocumentName}");
            TDocumentWithObject result = this.CreateInternal(dbObject, id);
            this.Logger?.Trace(this.GetType().Name, $"STORING NEW started for id {result.Id} and supplied Id {id} and to wait for creation {waitForCreation}. Document Name {this.DocumentName}");

            Func<bool> spinfun = () => this.Load(result.Id, true).Id == result.Id;
            if (waitForCreation)
            {
                this.Logger?.Trace(this.GetType().Name, $"Waiting until creation of {result.Id} and supplied Id {id} and to wait for creation {waitForCreation} is done. Document Name {this.DocumentName}");

                SpinWait.SpinUntil(spinfun, TimeSpan.FromSeconds(5));

                if (!spinfun())
                {
                    this.Logger?.Trace(this.GetType().Name, $"Error while waiting for creation id {result.Id}. Document Name {this.DocumentName}");
                    throw new Exception($"waitForCreation : Could not confirm creation of {result.Id}");
                }

                this.Logger?.Trace(this.GetType().Name, $"Created document {result.Id}. Document Name {this.DocumentName}");
                result = this.Load(result.Id, true);
            }

            return result;
        }

        /// <summary>
        ///     Id obtained must be used
        /// </summary>
        /// <returns></returns>
        public Tuple<string, long> GetNextId()
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to get next id . Document Name {this.DocumentName}");
            Interlocked.Increment(ref CurrentCount);

            string nextId = this.DocumentName + "-" + Guid.NewGuid().ToString(); //   ++CurrentCount;
            this.Logger?.Trace(this.GetType().Name, $"NEXT Id {nextId} CURRENT COUNT {CurrentCount} . Document Name {this.DocumentName}");
            return new Tuple<string, long>(nextId, CurrentCount);
        }

        public string LoadJson(string productId, bool includedDeletedDocuments = false, bool expectFullJsonStringReturned = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"loading id {productId} and include deleted : {includedDeletedDocuments} . Document Name {this.DocumentName}");
            return this.Load<string>(productId, includedDeletedDocuments, expectFullJsonStringReturned);
        }

        public TDocumentWithObject Load(string productId, bool includedDeletedDocuments = false, bool expectFullJsonStringReturned = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"loading id {productId} and include deleted : {includedDeletedDocuments} . Document Name {this.DocumentName}");
            return this.Load<TDocumentWithObject>(productId, includedDeletedDocuments, expectFullJsonStringReturned);
        }

        public T Load<T>(string productId, bool includedDeletedDocuments = false, bool expectFullJsonStringReturned = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"loading id {productId} . Document Name {this.DocumentName}");
            T value = default(T);
            if (!this.storageMechanism.Exists<TDocumentWithObject>(productId, this.DocumentName))
                return value;

            value = this.GET<T>(productId, expectFullJsonStringReturned);
            //todo perform this check BUG  a person can request deleted document by id
            //  if (value.Deleted && !includedDeletedDocuments)
            //    throw new Exception(productId + " document is already soft deleted");
            return value;
        }

        public string GetEtag(string productId, bool includedDeletedDocuments = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to get etag for {productId} . Document Name {this.DocumentName}");
            string etag = this.Load(productId, includedDeletedDocuments).ETag;
            this.Logger?.Trace(this.GetType().Name, $"Got etag for {productId} as {etag} . Document Name {this.DocumentName}");
            return etag;
        }

        public IEnumerable<TR> LoadAll<TR>(Func<IEnumerable<TDocumentWithObject>, IEnumerable<TR>> query)
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to load all. Document Name {this.DocumentName}");
            IEnumerable<TDocumentWithObject> result = this.storageMechanism.GetAll<TDocumentWithObject, TObjectOnly>(x => !x.Deleted, this.DocumentName);
            return query(result);
        }

        public IEnumerable<TDocumentWithObject> Search(Predicate<string> search, Func<IEnumerable<TDocumentWithObject>, IEnumerable<TDocumentWithObject>> query = null)

        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to search with predicate {search} . Document Name {this.DocumentName}");
            IEnumerable<TDocumentWithObject> result = this.storageMechanism.Search<TDocumentWithObject, TObjectOnly>(search, x => !x.Deleted, this.DocumentName);
            return query != null ? query(result) : result;
        }

        public IEnumerable<TDocumentWithObject> LoadAll(bool includeDeletedFiles = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to load all. include deleted files : {includeDeletedFiles} . Document Name {this.DocumentName}");
            IEnumerable<TDocumentWithObject> result = includeDeletedFiles ? this.storageMechanism.GetAll<TDocumentWithObject, TObjectOnly>(x => true, this.DocumentName) : this.storageMechanism.GetAll<TDocumentWithObject, TObjectOnly>(x => !x.Deleted, this.DocumentName);

            return result;
        }

        internal IEnumerable<string> LoadAllIdsIncludingDeletedDocuments()
        {
            this.Logger?.Trace(this.GetType().Name, $"Load all including deleted files . Document Name {this.DocumentName}");
            return this.storageMechanism.GetAll<TDocumentWithObject>(this.DocumentName);
        }

        public string Update(TDocumentWithObject dbObject, string etag = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to update document {dbObject?.Id} at position {dbObject?.Position} with etag supplied {etag} . Document Name {this.DocumentName}");
            TDocumentWithObject original = this.UpdateWithoutFlushAndHistory<TObjectOnly>(dbObject, false, null, etag);
            return original.ETag;
        }

        /// <summary>
        ///     It returns original
        /// </summary>
        /// <param name="dbObject"></param>
        /// <param name="createIfNoId"></param>
        /// <returns></returns>

        //public void UpdateorCreate(TDocumentWithObject dbObject)
        //{
        //     this.UpdateorCreateInternal(dbObject);
        //}
        /// <summary>
        ///     Markes data for deletion
        /// </summary>
        /// <param name="id"></param>
        public void Delete(string id, bool waitForDeletion = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to delete {id}. Wait for deletion {waitForDeletion} . Document Name {this.DocumentName}");

            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            TDocumentWithObject original = this.Load(id, true);
            original.Deleted = true;
            if (original.Id == null)
            {
            }

            this.Update(original);

            if (waitForDeletion)
            {
                Func<bool> spinfun = () => this.Load(id, true).Deleted;
                SpinWait.SpinUntil(spinfun, TimeSpan.FromSeconds(5));

                if (!spinfun())
                    throw new Exception($"waitForCreation : Could not confirm deletion of {id}");
            }
        }

        /// <summary>
        ///     Data cannot be deleted unless marked for deletion
        /// </summary>
        /// <param name="id"></param>
        public void DeleteForever(string id, string etag = null, bool waitForCreation = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Trying to delete {id} forever . Document Name {this.DocumentName}");
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            this.storageMechanism.Delete<TDocumentWithObject, TObjectOnly>(id, etag, this.DocumentName);

            if (waitForCreation)
            {
                Func<bool> spinfun = () => this.Load(id, true).Id == null;
                SpinWait.SpinUntil(spinfun, TimeSpan.FromSeconds(5));

                if (!spinfun())
                    throw new Exception($"waitForCreation : Could not confirm permanent deletion of {id}");
            }
        }

        public void CreateAll(List<TDocumentWithObject> dbObjects)
        {
            this.Logger?.Trace(this.GetType().Name, $"Creating all {dbObjects?.Count} documents. Document Name {this.DocumentName}");
            foreach (TDocumentWithObject dbObject in dbObjects)
                this.CreateAndFlush(dbObject);
        }

        public void UpdateAll(List<TDocumentWithObject> dbObjects)
        {
            this.Logger?.Trace(this.GetType().Name, $"Updating all {dbObjects?.Count} documents. Document Name {this.DocumentName}");

            foreach (TDocumentWithObject dbObject in dbObjects)
            {
                string id = (dbObject as IStandardDocument<TObjectOnly>)?.Id;

                if (id == null)
                    throw new Exception("Cannot perform the bulk update as one or more of the document is missing Id");
            }

            foreach (TDocumentWithObject dbObject in dbObjects)
                this.UpdateWithoutFlushAndHistory<TObjectOnly>(dbObject, false, null, dbObject.ETag);
        }

        #region INTERNALS

        TDocumentWithObject UpdateWithoutFlushAndHistory<TObjectOnly>(TDocumentWithObject dbObject, bool createIfNoId = false, string id = null, string etag = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Update without flush and history document {dbObject?.Id} at position {dbObject?.Position} with etag supplied {etag} . Document Name {this.DocumentName}");

            string ProductId = id ?? dbObject.Id;

            TDocumentWithObject original = default(TDocumentWithObject);
            if (createIfNoId && dbObject.Id == null)
            {
                dbObject.Id = id;
                //DocumentName createinternal does not preserve id even though it was set
                original = this.CreateInternal(dbObject, ProductId);
            }
            else
            {
                if (ProductId == null || !this.storageMechanism.Exists<TDocumentWithObject>(ProductId, this.DocumentName))
                    throw new Exception("Document doesnt exist with id " + ProductId);
                original = this.GET<TDocumentWithObject>(ProductId);
                this.SET(ProductId, dbObject, "UPDATE", etag);
            }

            return original;
        }

        internal TDocumentWithObject CreateInternal(TDocumentWithObject dbObject, string id = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Creating document {dbObject?.Id} at position {dbObject?.Position}  . Document Name {this.DocumentName}");

            TDocumentWithObject result = this.CreateAndFlush(dbObject, id);
            this.Logger?.Trace(this.GetType().Name, $"Creating document {dbObject?.Id} at position {dbObject?.Position}  . Document Name {this.DocumentName}");

            return result;
        }

        TDocumentWithObject CreateAndFlush(TDocumentWithObject dbObject, string id = null)
        {
            this.Logger?.Trace(this.GetType().Name, $"Create and flush document id {id} and id in document is {dbObject?.Id} at position {dbObject?.Position}. Document Name {this.DocumentName}");

            if (id != null && this.storageMechanism.Exists<TDocumentWithObject>(id, this.DocumentName))
            {
                this.Logger?.Error(this.GetType().Name, $"Error : document already exists while creating and flush document id {id} and id in document is {dbObject?.Id} at position {dbObject?.Position}. Document Name {this.DocumentName}");
                throw new InvalidOperationException($"Cannot create document with an id {id}:{this.DocumentName} that already exists : ");
            }

            if (!string.IsNullOrEmpty(id))
            {
                //todo check valid file name
                dbObject.Id = id;
                dbObject.Position = this.GetNextId().Item2;
            }
            else
            {
                Tuple<string, long> nid = this.GetNextId();
                dbObject.Id = nid.Item1;
                dbObject.Position = nid.Item2;
            }

            dbObject.DocumentName = this.DocumentName;
            dbObject.CreationDate = DateTime.UtcNow;
            this.SET(dbObject.Id, dbObject, "CREATE", null, true);
            return dbObject;
        }

        internal void SET(string productId, TDocumentWithObject dbObject, string context, string etag, bool noduplicates = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Calling set with id {productId} and id in document is {dbObject?.Id} in context {context} at position {dbObject?.Position} with etag supplied {etag} . Document Name {this.DocumentName}");

            var old = this.GET<TDocumentWithObject>(productId);

            this.storageMechanism.Set<TDocumentWithObject, TObjectOnly>(context, dbObject, productId, etag, this.DocumentName, noduplicates);

            SystemDbService.LogDocument<TDocumentWithObject, TObjectOnly>(productId, old, dbObject, context, this.storageMechanism, this.DocumentName, this.DataBaseSettings);
        }

        internal TTDocumentWithObject GET<TTDocumentWithObject>(string productId, bool expectFullJsonStringReturned = false)
        {
            this.Logger?.Trace(this.GetType().Name, $"Calling get with id {productId}. Document Name {this.DocumentName}");

            if (this.storageMechanism.Exists<TDocumentWithObject>(productId, this.DocumentName))
                return this.storageMechanism.Get<TTDocumentWithObject, TObjectOnly, TDocumentWithObject>(productId, this.DocumentName, expectFullJsonStringReturned);
            else
                return default(TTDocumentWithObject);
        }

        #endregion INTERNALS

        #region CONORS

        readonly DatabaseService DataBaseSettings;

        public StorageDatabase(IStorageMechanism storageMechanism, string documentName, DatabaseService dataBaseSettings, IPecanLogger logger)
        {
            this.Logger = logger;

            this.Logger?.Trace(this.GetType().Name, "Initializing storage database ...");

            this.DataBaseSettings = dataBaseSettings;
            this.storageMechanism = storageMechanism;

            string dname = PecanDatabaseUtilityObj.DetermineDatabaseName<TDocumentWithObject, TObjectOnly>(documentName);

            this.DocumentName = dname;

            CurrentCount = this.storageMechanism.Count<TDocumentWithObject>(this.DocumentName, true);

            this.Logger?.Trace(this.GetType().Name, $"Initialized storage. Document Name {this.DocumentName} and current count {CurrentCount}");
        }

        public void Dispose()
        {
            //DocumentName dispose by removing from db service as well
            //this.Data.Dispose();
        }

        //public Esent(string storageName)
        //{
        //    this.InstantiateDB(storageName);
        //}

        #endregion CONORS
    }
}