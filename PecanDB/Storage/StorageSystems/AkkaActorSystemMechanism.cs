namespace PecanDb.Storage.StorageSystems
{
    using Akka.Actor;
    using PecanDb.Storage.Contracts;
    using PecanDB;
    using System;
    using System.Collections.Generic;

    public class AkkaActorSystemMechanism : IStorageMechanism
    {
        private readonly IActorRef _storageActorRef;
        private readonly IStorageMechanism _worker;
        private readonly DataBaseSettings DataBaseSettings;

        public AkkaActorSystemMechanism(IActorRef storageActorRef, IStorageMechanism worker, DataBaseSettings DdtaBaseSettings, IPecanLogger logger)
        {
            this.Logger = logger;
            this._storageActorRef = storageActorRef;
            this._worker = worker;
            this.FileSystem = this._worker?.FileSystem;
            this.DataBaseSettings = DdtaBaseSettings;
        }

        public void Set<TDocumentWithObject, TObjectOnly>(string context, TDocumentWithObject Object, string id, string etag, string documentDirectory, bool noDuplicates)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Reactively queuing set for document id {id} and id etag is {etag} at position {Object?.Position}. Document Directory {documentDirectory}");

            Func<bool> operation = () =>
            {
                this.Logger?.Trace(this.GetType().Name, $"Working on set for document id {id} and id etag is {etag} at position {Object?.Position}. Document Directory {documentDirectory}");

                this._worker.Set<TDocumentWithObject, TObjectOnly>(context, Object, id, etag, documentDirectory, noDuplicates);
                return true;
            };

            if (this.DataBaseSettings.DontWaitForWritesOnCreate && context == "CREATE")
            {
                this._storageActorRef.Tell(operation);
            }
            else if (this.DataBaseSettings.DontWaitForWrites)
            {
                this._storageActorRef.Tell(operation);
            }
            else
            {
                object result = this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            }

            this.Logger?.Trace(this.GetType().Name, $"Work completed for document id {id} and id etag is {etag} at position {Object?.Position}. Document Directory {documentDirectory}");
        }

        public IStorageSystem FileSystem { get; set; }

        public IPecanLogger Logger { get; set; }

        public void InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(string documentDirectory)

        {
            this.Logger?.Trace(this.GetType().Name, $"Work Queued Initialize db cacahe if not ready for  Document Directory {documentDirectory}");
            Func<object> operation = () =>
            {
                this._worker.InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(documentDirectory);
                return true;
            };
            object result = this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            this.Logger?.Trace(this.GetType().Name, $"Work completed initialize db cache if not ready for  Document Directory {documentDirectory}");
        }

        public IEnumerable<TDocumentWithObject> Search<TDocumentWithObject, TObjectOnly>(Predicate<string> search, Func<TDocumentWithObject, bool> where, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Work Queued  search for  Document Directory {documentDirectory}");
            Func<object> operation = () => this._worker.Search<TDocumentWithObject, TObjectOnly>(search, where, documentDirectory);
            var result = (IEnumerable<TDocumentWithObject>)this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            this.Logger?.Trace(this.GetType().Name, $"Work completed search for  Document Directory {documentDirectory}");
            return result;
        }

        public TDocumentWithObject Get<TDocumentWithObject, TObjectOnly, TWithFileDocumentWithObject>(string id, string documentDirectory, bool expectFullJsonStringReturned)

        {
            this.Logger?.Trace(this.GetType().Name, $"Work queued get for document id {id}  . Document Directory {documentDirectory}");

            Func<object> operation = () => this._worker.Get<TDocumentWithObject, TObjectOnly, TWithFileDocumentWithObject>(id, documentDirectory, expectFullJsonStringReturned);
            var result = (TDocumentWithObject)this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            this.Logger?.Trace(this.GetType().Name, $"Work completed for document id {id} Document Directory {documentDirectory}");

            return result;
        }

        public IEnumerable<string> GetAll<TDocumentWithObject>(string documentDirectory)
        {
            this.Logger?.Trace(this.GetType().Name, $"Work queued get all for  Document Directory {documentDirectory}");
            Func<object> operation = () => this._worker.GetAll<TDocumentWithObject>(documentDirectory);
            var result = (IEnumerable<string>)this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            this.Logger?.Trace(this.GetType().Name, $"Work completed get all for  Document Directory {documentDirectory}");
            return result;
        }

        public IEnumerable<TDocumentWithObject> GetAll<TDocumentWithObject, TObjectOnly>(Func<TDocumentWithObject, bool> where, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Work queued get all for  Document Directory {documentDirectory}");
            Func<object> operation = () => this._worker.GetAll<TDocumentWithObject, TObjectOnly>(where, documentDirectory);
            var result = (IEnumerable<TDocumentWithObject>)this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            this.Logger?.Trace(this.GetType().Name, $"Work completed get all for  Document Directory {documentDirectory}");
            return result;
        }

        public long Count<TDocumentWithObject>(string documentDirectory, bool notFromCache)
        {
            this.Logger?.Trace(this.GetType().Name, $"Work queued count for  Document Directory {documentDirectory}");
            Func<object> operation = () => this._worker.Count<TDocumentWithObject>(documentDirectory, notFromCache);
            long result = (long)this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            this.Logger?.Trace(this.GetType().Name, $"Work completed count for  Document Directory {documentDirectory}");
            return result;
        }

        public bool Exists<TDocumentWithObject>(string id, string documentDirectory)
        {
            return this._worker.Exists<TDocumentWithObject>(id, documentDirectory);
        }

        public void Delete<TDocumentWithObject, TObjectOnly>(string id, string etag, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            this.Logger?.Trace(this.GetType().Name, $"Work queued delete for document id {id} and id etag is {etag} . Document Directory {documentDirectory}");

            Func<bool> operation = () =>
            {
                this._worker.Delete<TDocumentWithObject, TObjectOnly>(id, etag, documentDirectory);
                return true;
            };
            object result = this._storageActorRef.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            this.Logger?.Trace(this.GetType().Name, $"Work completed for document id {id} and id etag is {etag} . Document Directory {documentDirectory}");
        }
    }
}