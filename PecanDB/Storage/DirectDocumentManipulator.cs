namespace PecanDb.Storage
{
    using PecanDB;
    using System;

    public class DirectDocumentManipulator
    {
        private readonly DatabaseService DatabaseService;
        private readonly IStorageMechanism sMech;
        private IPecanLogger Logger;

        public DirectDocumentManipulator(IStorageMechanism s, DatabaseService _DatabaseService, IPecanLogger logger)
        {
            this.Logger = logger;
            this.DatabaseService = _DatabaseService;
            this.sMech = s ?? throw new ArgumentNullException(nameof(s));
        }

        public StorageDatabase<TDocumentWithObject, TObjectOnly> GetDBRef<TDocumentWithObject, TObjectOnly>(string d = null)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>

        {
            return this.DatabaseService.DatabaseAccessRegardlessOfTransaction<TDocumentWithObject, TObjectOnly>(this.sMech, d);
        }
    }
}