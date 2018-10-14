namespace WPecanTests
{
    using PecanDb.Storage;
    using System;

    [Serializable]
    public class Finance<R> : IStandardDocument<R>
    {
        public string FileName { get; set; }

        public string FileDescription { get; set; }

        public string FilePath { get; set; }

        public string FileContent { get; set; }

        public string Id { get; set; }

        public bool Deleted { get; set; }

        public DateTime CreationDate { get; set; }

        public string ETag { get; set; }

        public R DocumentEntity { get; set; }

        public string DocumentName { get; set; }

        public long Position { get; set; }
    }
}