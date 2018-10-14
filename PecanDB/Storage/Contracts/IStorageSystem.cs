namespace PecanDb.Storage.Contracts
{
    using PecanDb.Storage.StorageSystems;
    using PecanDB;
    using System;
    using System.Collections.Generic;

    public interface IStorageSystem
    {
        ISerializationFactory SerializationFactory { set; get; }

        IPecanLogger Logger { set; get; }

        void InitializeDatabaseCacheIfNotReady<TDocumentWithObject>(string getDbFolder);

        // string ReadFile<TDocumentWithObject>(string fileName);
        TDocumentWithObject ReadObjectFromFile<TDocumentWithObject, TFromFileTDocumentWithObject>(string fileName, bool ignoreCaching = false);

        IEnumerable<TDocumentWithObject> GetAll<TDocumentWithObject, TObjectOnly>(string folder, Func<TDocumentWithObject, bool> where, Predicate<string> search = null)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>;

        // void WriteToFile(string fileName, string content);
        IEnumerable<string> EnumerateFilesNotFromCache<TDocumentWithObject>(string getDbFolder);

        void WriteToFile<TDocumentWithObject>(string fileNme, TDocumentWithObject content);

        IEnumerable<string> EnumerateFiles<TDocumentWithObject>(string getDbFolder);

        bool DirectoryExists(string db);

        void CreateDirectory(string db);

        bool FileExists<TDocumentWithObject>(string toFileName, bool ignoreCache);

        void FileDelete<TDocumentWithObject>(string toFileName);

        string ReadJsonFromFile<T, T1>(string name, bool ignoreCaching = false);
    }
}