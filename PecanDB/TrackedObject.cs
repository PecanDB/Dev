namespace PecanDB
{
    using System;

    internal class TrackedObject
    {
        public TrackedObject(object doc, Type docType, string etag, string documentName, bool isNew)
        {
            this.Document = doc;
            this.DocumentType = docType;
            this.ETag = etag;
            this.DocumentName = documentName;
            this.IsNew = isNew;
        }

        public object Document { set; get; }

        public Type DocumentType { set; get; }

        public string ETag { set; get; }

        public string DocumentName { set; get; }

        public bool IsNew { set; get; }
    }
}