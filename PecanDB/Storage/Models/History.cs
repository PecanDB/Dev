namespace PecanDb.Storage
{
    using System;

    [Serializable]
    public class History<TObjectOnly> : IStandardDocument<TObjectOnly>, IPropertyCompareResult
    {
        public string Name { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }

        public DateTime DateTime { get; set; }

        public string DocumentName { get; set; }

        public long Position { get; set; }

        public string Id { get; set; }

        public bool Deleted { get; set; }

        public DateTime CreationDate { get; set; }

        public string ETag { get; set; }

        public TObjectOnly DocumentEntity { get; set; }
    }
}