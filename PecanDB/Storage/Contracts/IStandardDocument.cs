namespace PecanDb.Storage
{
    using System;

    public interface IStandardDocument<TObjectOnly>
    {
        [Obsolete("Avoid use of this property")]
        string Id { set; get; }

        [Obsolete("Avoid use of this property")]
        bool Deleted { set; get; }

        [Obsolete("Avoid use of this property")]
        DateTime CreationDate { set; get; }

        [Obsolete("Avoid use of this property")]
        string ETag { set; get; }

        TObjectOnly DocumentEntity { set; get; }

        string DocumentName { set; get; }

        long Position { set; get; }
    }
}