namespace PecanDb.Storage
{
    using PecanDB;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SystemDbService
    {
        public static DbStats GetSystemStatistics<TObjectOnly>(string documentName, DatabaseService DatabaseService)
        {
            return GetSystemStatistics(DatabaseService, null, documentName);
        }

        public static DbStats GetSystemStatistics(DatabaseService DatabaseService, Func<SystemDb<object>, bool> query = null, string documentName = "")
        {
            var dbStats = new DbStats();
            StorageDatabase<SystemDb<object>, object> sys = DatabaseService.Documents<SystemDb<object>, object>(null);

            var docs = new List<SystemDb<object>>();
            foreach (string s in sys.LoadAllIdsIncludingDeletedDocuments())
                docs.Add(sys.Load(s));
            docs = docs.Where(
                x =>
                    x.DocumentName.ToLower() != documentName.ToLower() && true ||
                    x.DocumentName.ToLower() == documentName.ToLower()
            ).ToList();

            if (query != null)
                docs = docs.Where(query).ToList();

            int HistoryCount = docs.Count(x => x.DocumentName == TypeOfWrapper.TypeOf(typeof(History<>)).Name);
            dbStats.TotalDocumentCount = docs.Count - HistoryCount;
            dbStats.DeletedDocumentCount = docs.Count(x => x.Deleted);

            dbStats.HistoryDocumentCount = HistoryCount;

            return dbStats;
        }

        public static void LogDocument<TDocumentWithObject, TObjectOnly>(string documentId, TDocumentWithObject oldDoc, TDocumentWithObject newDoc, string operation, IStorageMechanism storageMechanism, string databaseName, DatabaseService DatabaseService)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            if (documentId == null)
                throw new ArgumentNullException(nameof(documentId));
            if (TypeOfWrapper.TypeOf(typeof(TDocumentWithObject)).Name != TypeOfWrapper.TypeOf(typeof(History<>)).Name)
                HistoryService.UpdateDocumentHistory<TDocumentWithObject, TObjectOnly>(oldDoc, newDoc, storageMechanism, databaseName, DatabaseService);

            if (TypeOfWrapper.TypeOf(typeof(TDocumentWithObject)).Name == TypeOfWrapper.TypeOf(typeof(SystemDb<>)).Name)
                return;
        }
    }
}