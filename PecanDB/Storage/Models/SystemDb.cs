namespace PecanDb.Storage
{
    using System;

    [Serializable]
    public class SystemDb<TObjectOnly> : IStandardDocument<TObjectOnly>
    {
        public DateTime LastAccessed { get; set; }

        public string DocumentId { set; get; }

        public string LastOperation { set; get; }

        public string DocumentName { set; get; }

        public long Position { get; set; }

        public string Id { get; set; }

        public bool Deleted { get; set; }

        public DateTime CreationDate { get; set; }

        public string ETag { get; set; }

        public TObjectOnly DocumentEntity { get; set; }
    }
}