namespace PecanDB
{
    public class FilesStorage
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string FilePath { set; get; }

        public string FileType { set; get; }

        public string OriginalPath { get; set; }

        public string TextFromAttachment { set; get; }
    }
}