namespace PecanDB
{
    using PecanDb.Storage;
    using System;

    public class PecanDocument<T> : IStandardDocument<T>

    {
        public string Id { set; get; }

        public bool Deleted { set; get; }

        public DateTime CreationDate { set; get; }

        public string ETag { set; get; }

        public T DocumentEntity { set; get; }

        public string DocumentName { set; get; }

        public long Position { get; set; }
    }
}