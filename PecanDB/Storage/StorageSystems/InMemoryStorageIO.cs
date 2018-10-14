namespace PecanDb.Storage.StorageSystems
{
    using PecanDB;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class InMemoryStorageIO : IStorageIO
    {
        public static ConcurrentDictionary<InMemoryFileStructure, string> Files = new ConcurrentDictionary<InMemoryFileStructure, string>();

        public InMemoryStorageIO(IPecanLogger logger)
        {
            this.Logger = logger;
        }

        public IPecanLogger Logger { get; set; }

        public IEnumerable<string> DirectoryEnumerateFiles(string name)
        {
            this.Logger?.Trace(this.GetType().Name, $" Enumerating files in {name} In memory file io ");

            string p = this.GetDirectoryName(name);
            return Files.Where(x => x.Key.DirectoryName == p).Select(x => x.Key.FileName);
        }

        public bool DirectoryExists(string db)
        {
            this.Logger?.Trace(this.GetType().Name, $" Check if directory exists in {db} In memory file io ");
            string p = this.GetDirectoryName(db);
            return Files.Count(x => x.Key.DirectoryName == p) != 0;
        }

        public bool FileExists(string db)
        {
            this.Logger?.Trace(this.GetType().Name, $"Check if file {db} exists  In memory file io ");
            return Files.Count(x => x.Key.FileName == db) != 0;
        }

        public void FileDelete(string db)
        {
            this.Logger?.Trace(this.GetType().Name, $" Delete file {db} In memory file io ");

            KeyValuePair<InMemoryFileStructure, string> found = Files.First(x => x.Key.FileName == db);
            string s;
            Files.TryRemove(found.Key, out s);
        }

        public void CreateDirectory(string db)
        {
            this.Logger?.Trace(this.GetType().Name, $" Create directory {db} In memory file io ");
        }

        public string FileReadAllText(string fileName)
        {
            this.Logger?.Trace(this.GetType().Name, $" Read all text from file {fileName} In memory file io ");
            return Files.FirstOrDefault(x => x.Key.FileName == fileName).Value;
        }

        public void FileWriteAllText(string fileName, string content)
        {
            this.Logger?.Trace(this.GetType().Name, $"Write all text to file {fileName} In memory file io ", null, content);
            var key = new InMemoryFileStructure
            {
                FileName = fileName,
                DirectoryName = this.GetDirectoryName(fileName)
            };

            if (this.FileExists(fileName))
                Files[key] = content;
            else
                Files.TryAdd(key, content);
        }

        private string GetDirectoryName(string path)
        {
            this.Logger?.Trace(this.GetType().Name, $"Get directory name in path {path} In memory file io ");
            string p = Path.GetFullPath(path);
            return Path.HasExtension(p) ? Path.GetDirectoryName(p) : p;
        }

        public class InMemoryFileStructure
        {
            public string DirectoryName { set; get; }

            public string FileName { set; get; }
        }
    }
}