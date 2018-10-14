namespace PecanDb.Storage
{
    using PecanDb.Storage.Contracts;
    using PecanDB;
    using System;
    using System.Collections.Generic;

    public interface IStorageMechanism
    {
        IStorageSystem FileSystem { set; get; }

        IPecanLogger Logger { set; get; }

        void InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(string documentDirectory);

        void Set<TDocumentWithObject, TObjectOnly>(string context, TDocumentWithObject Object, string id, string etag, string documentDirectory, bool noDuplicates)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>;

        IEnumerable<TDocumentWithObject> Search<TDocumentWithObject, TObjectOnly>(Predicate<string> search, Func<TDocumentWithObject, bool> where, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>;

        TDocumentWithObject Get<TDocumentWithObject, TObjectOnly, TWithFileDocumentWithObject>(string id, string documentDirectory, bool expectFullJsonStringReturned);

        IEnumerable<string> GetAll<TDocumentWithObject>(string documentDirectory);

        IEnumerable<TDocumentWithObject> GetAll<TDocumentWithObject, TObjectOnly>(Func<TDocumentWithObject, bool> where, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>;

        long Count<TDocumentWithObject>(string documentDirectory, bool notFromCache);

        bool Exists<TDocumentWithObject>(string id, string documentDirectory);

        void Delete<TDocumentWithObject, TObjectOnly>(string id, string etag, string documentDirectory)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>;
    }
}