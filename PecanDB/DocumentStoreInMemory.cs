namespace PecanDB
{
    using System;

    public class DocumentStoreInMemory : PecanDocumentStore
    {
        public DocumentStoreInMemory(DatabaseOptions databaseOptions = null)
            : base(Guid.NewGuid().ToString(), true, databaseOptions)
        {
        }
    }
}