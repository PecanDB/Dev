namespace PecanDb.Storage
{
    public class DbStats
    {
        public int DeletedDocumentCount { get; set; }

        public int TotalDocumentCount { get; set; }

        public int HistoryDocumentCount { get; set; }
    }
}